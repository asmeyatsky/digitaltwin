"""
Avatar Generation Service
Generates 3D avatars from user photos using face reconstruction ML models
"""

import io
import os
import uuid
import logging
import tempfile
from typing import Optional
from pathlib import Path
from contextlib import asynccontextmanager

import numpy as np
from fastapi import FastAPI, HTTPException, UploadFile, File, BackgroundTasks
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse, JSONResponse
from pydantic import BaseModel
from PIL import Image
import cv2

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Storage paths
AVATAR_STORAGE_PATH = os.getenv("AVATAR_STORAGE_PATH", "/app/avatars")
TEMP_PATH = os.getenv("TEMP_PATH", "/tmp/avatar-gen")

# Ensure directories exist
Path(AVATAR_STORAGE_PATH).mkdir(parents=True, exist_ok=True)
Path(TEMP_PATH).mkdir(parents=True, exist_ok=True)

# Lazy-loaded ML models
face_mesh = None
face_detector = None


def get_mediapipe():
    """Lazy load MediaPipe for face mesh extraction"""
    global face_mesh, face_detector
    if face_mesh is None:
        import mediapipe as mp
        mp_face_mesh = mp.solutions.face_mesh
        face_mesh = mp_face_mesh.FaceMesh(
            static_image_mode=True,
            max_num_faces=1,
            refine_landmarks=True,
            min_detection_confidence=0.5
        )
        mp_face_detection = mp.solutions.face_detection
        face_detector = mp_face_detection.FaceDetection(
            model_selection=1,
            min_detection_confidence=0.5
        )
        logger.info("MediaPipe models loaded")
    return face_mesh, face_detector


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Application lifespan handler"""
    logger.info("Starting Avatar Generation Service...")
    # Pre-load models
    try:
        get_mediapipe()
        logger.info("ML models pre-loaded successfully")
    except Exception as e:
        logger.warning(f"Model pre-loading skipped: {e}")
    yield
    logger.info("Shutting down Avatar Generation Service...")


app = FastAPI(
    title="Avatar Generation Service",
    description="Generate 3D avatars from user photos using ML face reconstruction",
    version="1.0.0",
    lifespan=lifespan
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


# Response models
class AvatarGenerationRequest(BaseModel):
    """Request for avatar generation"""
    user_id: str
    avatar_style: str = "realistic"  # realistic, stylized, cartoon
    include_body: bool = False
    output_format: str = "glb"  # glb, fbx, obj


class AvatarGenerationResponse(BaseModel):
    """Response from avatar generation"""
    avatar_id: str
    user_id: str
    status: str
    avatar_url: Optional[str] = None
    thumbnail_url: Optional[str] = None
    face_landmarks_count: int = 0
    mesh_vertices: int = 0
    texture_resolution: str = "1024x1024"
    error: Optional[str] = None


class AvatarStatus(BaseModel):
    """Avatar generation status"""
    avatar_id: str
    status: str  # pending, processing, completed, failed
    progress: float = 0.0
    message: Optional[str] = None


class FaceLandmarks(BaseModel):
    """Facial landmarks data"""
    landmarks: list[dict]
    face_oval: list[dict]
    left_eye: list[dict]
    right_eye: list[dict]
    lips: list[dict]
    nose: list[dict]
    face_bounds: dict


class AvatarMeshData(BaseModel):
    """3D mesh data for avatar"""
    vertices: list[list[float]]
    faces: list[list[int]]
    uvs: list[list[float]]
    normals: list[list[float]]


# In-memory status tracking (use Redis in production)
avatar_jobs = {}


@app.get("/health")
async def health_check():
    """Health check endpoint"""
    return {
        "status": "healthy",
        "service": "avatar-generation-service",
        "version": "1.0.0",
        "models_loaded": face_mesh is not None
    }


@app.post("/avatar/generate", response_model=AvatarGenerationResponse)
async def generate_avatar(
    file: UploadFile = File(...),
    user_id: str = "anonymous",
    avatar_style: str = "realistic",
    background_tasks: BackgroundTasks = None
):
    """
    Generate a 3D avatar from a user photo.

    Args:
        file: User photo (face clearly visible)
        user_id: User identifier for avatar association
        avatar_style: Style of avatar (realistic, stylized, cartoon)

    Returns:
        AvatarGenerationResponse with avatar details and download URL
    """
    avatar_id = str(uuid.uuid4())

    try:
        # Read and validate image
        contents = await file.read()
        image = Image.open(io.BytesIO(contents))

        # Convert to RGB
        if image.mode != 'RGB':
            image = image.convert('RGB')

        img_array = np.array(image)

        # Detect face and extract landmarks
        face_mesh_model, face_detector_model = get_mediapipe()

        # Convert BGR to RGB for MediaPipe
        rgb_image = cv2.cvtColor(img_array, cv2.COLOR_RGB2BGR)
        rgb_image = cv2.cvtColor(rgb_image, cv2.COLOR_BGR2RGB)

        # Detect face
        detection_results = face_detector_model.process(rgb_image)
        if not detection_results.detections:
            raise HTTPException(status_code=400, detail="No face detected in image")

        # Extract face mesh landmarks
        mesh_results = face_mesh_model.process(rgb_image)
        if not mesh_results.multi_face_landmarks:
            raise HTTPException(status_code=400, detail="Could not extract facial landmarks")

        face_landmarks = mesh_results.multi_face_landmarks[0]

        # Extract landmark coordinates
        h, w = img_array.shape[:2]
        landmarks_3d = []
        for landmark in face_landmarks.landmark:
            landmarks_3d.append({
                'x': landmark.x * w,
                'y': landmark.y * h,
                'z': landmark.z * w  # Scale z similarly
            })

        # Generate 3D mesh from landmarks
        mesh_data = generate_face_mesh(landmarks_3d, img_array.shape)

        # Extract and process face texture
        texture_path = extract_face_texture(img_array, landmarks_3d, avatar_id)

        # Generate avatar files (GLB format for Unity)
        avatar_path = generate_avatar_glb(
            mesh_data,
            texture_path,
            avatar_id,
            avatar_style
        )

        # Generate thumbnail
        thumbnail_path = generate_thumbnail(img_array, avatar_id)

        # Store avatar metadata
        avatar_jobs[avatar_id] = {
            "status": "completed",
            "user_id": user_id,
            "avatar_path": avatar_path,
            "texture_path": texture_path,
            "thumbnail_path": thumbnail_path
        }

        return AvatarGenerationResponse(
            avatar_id=avatar_id,
            user_id=user_id,
            status="completed",
            avatar_url=f"/avatar/{avatar_id}/download",
            thumbnail_url=f"/avatar/{avatar_id}/thumbnail",
            face_landmarks_count=len(landmarks_3d),
            mesh_vertices=len(mesh_data["vertices"]),
            texture_resolution="1024x1024"
        )

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error generating avatar: {e}")
        avatar_jobs[avatar_id] = {"status": "failed", "error": str(e)}
        raise HTTPException(status_code=500, detail=f"Avatar generation failed: {str(e)}")


@app.post("/avatar/extract-landmarks", response_model=FaceLandmarks)
async def extract_landmarks(file: UploadFile = File(...)):
    """
    Extract facial landmarks from an image without generating full avatar.
    Useful for real-time face tracking and expression mapping.
    """
    try:
        contents = await file.read()
        image = Image.open(io.BytesIO(contents))

        if image.mode != 'RGB':
            image = image.convert('RGB')

        img_array = np.array(image)
        h, w = img_array.shape[:2]

        face_mesh_model, _ = get_mediapipe()
        rgb_image = cv2.cvtColor(img_array, cv2.COLOR_RGB2BGR)
        rgb_image = cv2.cvtColor(rgb_image, cv2.COLOR_BGR2RGB)

        results = face_mesh_model.process(rgb_image)
        if not results.multi_face_landmarks:
            raise HTTPException(status_code=400, detail="No face detected")

        face_landmarks = results.multi_face_landmarks[0]

        # Extract all landmarks
        all_landmarks = []
        for i, landmark in enumerate(face_landmarks.landmark):
            all_landmarks.append({
                'index': i,
                'x': landmark.x * w,
                'y': landmark.y * h,
                'z': landmark.z * w
            })

        # MediaPipe face mesh indices for specific features
        # Face oval
        face_oval_indices = [10, 338, 297, 332, 284, 251, 389, 356, 454, 323, 361, 288,
                           397, 365, 379, 378, 400, 377, 152, 148, 176, 149, 150, 136,
                           172, 58, 132, 93, 234, 127, 162, 21, 54, 103, 67, 109]

        # Left eye
        left_eye_indices = [33, 7, 163, 144, 145, 153, 154, 155, 133, 173, 157, 158, 159, 160, 161, 246]

        # Right eye
        right_eye_indices = [362, 382, 381, 380, 374, 373, 390, 249, 263, 466, 388, 387, 386, 385, 384, 398]

        # Lips
        lips_indices = [61, 146, 91, 181, 84, 17, 314, 405, 321, 375, 291, 308, 324, 318, 402, 317, 14, 87, 178, 88, 95]

        # Nose
        nose_indices = [1, 2, 98, 327, 168, 6, 197, 195, 5, 4, 19, 94, 2]

        def get_landmarks_by_indices(indices):
            return [all_landmarks[i] for i in indices if i < len(all_landmarks)]

        # Calculate face bounds
        xs = [lm['x'] for lm in all_landmarks]
        ys = [lm['y'] for lm in all_landmarks]

        return FaceLandmarks(
            landmarks=all_landmarks,
            face_oval=get_landmarks_by_indices(face_oval_indices),
            left_eye=get_landmarks_by_indices(left_eye_indices),
            right_eye=get_landmarks_by_indices(right_eye_indices),
            lips=get_landmarks_by_indices(lips_indices),
            nose=get_landmarks_by_indices(nose_indices),
            face_bounds={
                'x': min(xs),
                'y': min(ys),
                'width': max(xs) - min(xs),
                'height': max(ys) - min(ys)
            }
        )

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error extracting landmarks: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/avatar/{avatar_id}/status", response_model=AvatarStatus)
async def get_avatar_status(avatar_id: str):
    """Get the status of an avatar generation job"""
    if avatar_id not in avatar_jobs:
        raise HTTPException(status_code=404, detail="Avatar not found")

    job = avatar_jobs[avatar_id]
    return AvatarStatus(
        avatar_id=avatar_id,
        status=job.get("status", "unknown"),
        progress=1.0 if job.get("status") == "completed" else 0.5,
        message=job.get("error")
    )


@app.get("/avatar/{avatar_id}/download")
async def download_avatar(avatar_id: str):
    """Download the generated avatar GLB file"""
    if avatar_id not in avatar_jobs:
        raise HTTPException(status_code=404, detail="Avatar not found")

    job = avatar_jobs[avatar_id]
    if job.get("status") != "completed":
        raise HTTPException(status_code=400, detail="Avatar not ready")

    avatar_path = job.get("avatar_path")
    if not avatar_path or not os.path.exists(avatar_path):
        raise HTTPException(status_code=404, detail="Avatar file not found")

    return FileResponse(
        avatar_path,
        media_type="model/gltf-binary",
        filename=f"avatar_{avatar_id}.glb"
    )


@app.get("/avatar/{avatar_id}/thumbnail")
async def get_avatar_thumbnail(avatar_id: str):
    """Get avatar thumbnail image"""
    if avatar_id not in avatar_jobs:
        raise HTTPException(status_code=404, detail="Avatar not found")

    job = avatar_jobs[avatar_id]
    thumbnail_path = job.get("thumbnail_path")

    if not thumbnail_path or not os.path.exists(thumbnail_path):
        raise HTTPException(status_code=404, detail="Thumbnail not found")

    return FileResponse(thumbnail_path, media_type="image/png")


@app.get("/avatar/{avatar_id}/texture")
async def get_avatar_texture(avatar_id: str):
    """Get avatar face texture"""
    if avatar_id not in avatar_jobs:
        raise HTTPException(status_code=404, detail="Avatar not found")

    job = avatar_jobs[avatar_id]
    texture_path = job.get("texture_path")

    if not texture_path or not os.path.exists(texture_path):
        raise HTTPException(status_code=404, detail="Texture not found")

    return FileResponse(texture_path, media_type="image/png")


@app.delete("/avatar/{avatar_id}")
async def delete_avatar(avatar_id: str):
    """Delete an avatar and its associated files"""
    if avatar_id not in avatar_jobs:
        raise HTTPException(status_code=404, detail="Avatar not found")

    job = avatar_jobs[avatar_id]

    # Delete files
    for key in ["avatar_path", "texture_path", "thumbnail_path"]:
        path = job.get(key)
        if path and os.path.exists(path):
            os.remove(path)

    del avatar_jobs[avatar_id]
    return {"status": "deleted", "avatar_id": avatar_id}


# Helper functions for avatar generation

def generate_face_mesh(landmarks: list, image_shape: tuple) -> dict:
    """
    Generate a 3D face mesh from facial landmarks.
    Uses Delaunay triangulation for face surface.
    """
    import scipy.spatial as spatial

    h, w = image_shape[:2]

    # Convert landmarks to vertices
    vertices = []
    for lm in landmarks:
        # Normalize to -1 to 1 range for 3D space
        x = (lm['x'] / w) * 2 - 1
        y = -((lm['y'] / h) * 2 - 1)  # Flip Y for 3D coordinate system
        z = lm['z'] / w  # Depth
        vertices.append([x, y, z])

    vertices = np.array(vertices)

    # Create 2D points for triangulation (ignore z)
    points_2d = vertices[:, :2]

    # Perform Delaunay triangulation
    try:
        tri = spatial.Delaunay(points_2d)
        faces = tri.simplices.tolist()
    except Exception as e:
        logger.warning(f"Delaunay triangulation failed: {e}, using fallback")
        faces = generate_fallback_faces(len(vertices))

    # Generate UV coordinates (normalized landmark positions)
    uvs = []
    for lm in landmarks:
        u = lm['x'] / w
        v = 1.0 - (lm['y'] / h)  # Flip V for texture coordinates
        uvs.append([u, v])

    # Calculate normals for each vertex
    normals = calculate_vertex_normals(vertices, faces)

    return {
        "vertices": vertices.tolist(),
        "faces": faces,
        "uvs": uvs,
        "normals": normals
    }


def generate_fallback_faces(num_vertices: int) -> list:
    """Generate simple triangle fan as fallback"""
    faces = []
    center = 0  # Use first vertex as center
    for i in range(1, num_vertices - 1):
        faces.append([center, i, i + 1])
    return faces


def calculate_vertex_normals(vertices: np.ndarray, faces: list) -> list:
    """Calculate smooth vertex normals"""
    normals = np.zeros_like(vertices)

    for face in faces:
        if len(face) < 3:
            continue
        v0, v1, v2 = vertices[face[0]], vertices[face[1]], vertices[face[2]]

        # Calculate face normal
        edge1 = v1 - v0
        edge2 = v2 - v0
        face_normal = np.cross(edge1, edge2)

        # Add to vertex normals
        for idx in face:
            normals[idx] += face_normal

    # Normalize
    norms = np.linalg.norm(normals, axis=1, keepdims=True)
    norms[norms == 0] = 1  # Avoid division by zero
    normals = normals / norms

    return normals.tolist()


def extract_face_texture(image: np.ndarray, landmarks: list, avatar_id: str) -> str:
    """
    Extract and unwrap face texture from image.
    Creates a UV-mapped texture suitable for the generated mesh.
    """
    h, w = image.shape[:2]

    # Create texture image (1024x1024)
    texture_size = 1024
    texture = np.zeros((texture_size, texture_size, 3), dtype=np.uint8)

    # Get face bounding box from landmarks
    xs = [lm['x'] for lm in landmarks]
    ys = [lm['y'] for lm in landmarks]

    x_min, x_max = int(min(xs)), int(max(xs))
    y_min, y_max = int(min(ys)), int(max(ys))

    # Add padding
    padding = int((x_max - x_min) * 0.2)
    x_min = max(0, x_min - padding)
    x_max = min(w, x_max + padding)
    y_min = max(0, y_min - padding)
    y_max = min(h, y_max + padding)

    # Crop and resize face region
    face_region = image[y_min:y_max, x_min:x_max]
    face_resized = cv2.resize(face_region, (texture_size, texture_size))

    # Apply to texture
    texture = face_resized

    # Save texture
    texture_path = os.path.join(AVATAR_STORAGE_PATH, f"{avatar_id}_texture.png")
    cv2.imwrite(texture_path, cv2.cvtColor(texture, cv2.COLOR_RGB2BGR))

    return texture_path


def generate_avatar_glb(mesh_data: dict, texture_path: str, avatar_id: str, style: str) -> str:
    """
    Generate a GLB (binary glTF) file from mesh data and texture.
    GLB format is well-supported by Unity.
    """
    import struct
    import json

    vertices = mesh_data["vertices"]
    faces = mesh_data["faces"]
    uvs = mesh_data["uvs"]
    normals = mesh_data["normals"]

    # Read texture
    with open(texture_path, 'rb') as f:
        texture_data = f.read()

    # Build glTF structure
    gltf = {
        "asset": {"version": "2.0", "generator": "DigitalTwin Avatar Service"},
        "scene": 0,
        "scenes": [{"nodes": [0]}],
        "nodes": [{"mesh": 0, "name": "AvatarHead"}],
        "meshes": [{
            "primitives": [{
                "attributes": {
                    "POSITION": 0,
                    "NORMAL": 1,
                    "TEXCOORD_0": 2
                },
                "indices": 3,
                "material": 0
            }],
            "name": "FaceMesh"
        }],
        "materials": [{
            "pbrMetallicRoughness": {
                "baseColorTexture": {"index": 0},
                "metallicFactor": 0.0,
                "roughnessFactor": 0.8
            },
            "name": "FaceMaterial"
        }],
        "textures": [{"source": 0, "sampler": 0}],
        "images": [{"bufferView": 4, "mimeType": "image/png"}],
        "samplers": [{"magFilter": 9729, "minFilter": 9987, "wrapS": 10497, "wrapT": 10497}],
        "accessors": [],
        "bufferViews": [],
        "buffers": []
    }

    # Build binary buffer
    buffer_data = bytearray()

    # Positions (accessor 0)
    positions_data = bytearray()
    pos_min = [float('inf')] * 3
    pos_max = [float('-inf')] * 3
    for v in vertices:
        for i in range(3):
            pos_min[i] = min(pos_min[i], v[i])
            pos_max[i] = max(pos_max[i], v[i])
        positions_data.extend(struct.pack('fff', *v))

    positions_offset = len(buffer_data)
    buffer_data.extend(positions_data)

    # Pad to 4-byte alignment
    while len(buffer_data) % 4 != 0:
        buffer_data.append(0)

    # Normals (accessor 1)
    normals_data = bytearray()
    for n in normals:
        normals_data.extend(struct.pack('fff', *n))

    normals_offset = len(buffer_data)
    buffer_data.extend(normals_data)

    while len(buffer_data) % 4 != 0:
        buffer_data.append(0)

    # UVs (accessor 2)
    uvs_data = bytearray()
    for uv in uvs:
        uvs_data.extend(struct.pack('ff', *uv))

    uvs_offset = len(buffer_data)
    buffer_data.extend(uvs_data)

    while len(buffer_data) % 4 != 0:
        buffer_data.append(0)

    # Indices (accessor 3)
    indices_data = bytearray()
    index_count = 0
    for face in faces:
        for idx in face:
            indices_data.extend(struct.pack('H', idx))  # unsigned short
            index_count += 1

    indices_offset = len(buffer_data)
    buffer_data.extend(indices_data)

    while len(buffer_data) % 4 != 0:
        buffer_data.append(0)

    # Texture image (bufferView 4)
    texture_offset = len(buffer_data)
    buffer_data.extend(texture_data)

    while len(buffer_data) % 4 != 0:
        buffer_data.append(0)

    # Define buffer views
    gltf["bufferViews"] = [
        {"buffer": 0, "byteOffset": positions_offset, "byteLength": len(positions_data), "target": 34962},
        {"buffer": 0, "byteOffset": normals_offset, "byteLength": len(normals_data), "target": 34962},
        {"buffer": 0, "byteOffset": uvs_offset, "byteLength": len(uvs_data), "target": 34962},
        {"buffer": 0, "byteOffset": indices_offset, "byteLength": len(indices_data), "target": 34963},
        {"buffer": 0, "byteOffset": texture_offset, "byteLength": len(texture_data)}
    ]

    # Define accessors
    gltf["accessors"] = [
        {"bufferView": 0, "componentType": 5126, "count": len(vertices), "type": "VEC3",
         "min": pos_min, "max": pos_max},
        {"bufferView": 1, "componentType": 5126, "count": len(normals), "type": "VEC3"},
        {"bufferView": 2, "componentType": 5126, "count": len(uvs), "type": "VEC2"},
        {"bufferView": 3, "componentType": 5123, "count": index_count, "type": "SCALAR"}
    ]

    # Define buffer
    gltf["buffers"] = [{"byteLength": len(buffer_data)}]

    # Create GLB file
    gltf_json = json.dumps(gltf, separators=(',', ':')).encode('utf-8')

    # Pad JSON to 4-byte alignment
    while len(gltf_json) % 4 != 0:
        gltf_json += b' '

    # GLB header
    glb_data = bytearray()
    glb_data.extend(b'glTF')  # magic
    glb_data.extend(struct.pack('I', 2))  # version
    total_length = 12 + 8 + len(gltf_json) + 8 + len(buffer_data)
    glb_data.extend(struct.pack('I', total_length))  # total length

    # JSON chunk
    glb_data.extend(struct.pack('I', len(gltf_json)))  # chunk length
    glb_data.extend(b'JSON')  # chunk type
    glb_data.extend(gltf_json)

    # Binary chunk
    glb_data.extend(struct.pack('I', len(buffer_data)))  # chunk length
    glb_data.extend(b'BIN\x00')  # chunk type
    glb_data.extend(buffer_data)

    # Save GLB file
    glb_path = os.path.join(AVATAR_STORAGE_PATH, f"{avatar_id}.glb")
    with open(glb_path, 'wb') as f:
        f.write(glb_data)

    logger.info(f"Generated GLB avatar: {glb_path}")
    return glb_path


def generate_thumbnail(image: np.ndarray, avatar_id: str) -> str:
    """Generate a thumbnail image for the avatar"""
    thumbnail_size = (256, 256)
    thumbnail = cv2.resize(image, thumbnail_size)
    thumbnail_path = os.path.join(AVATAR_STORAGE_PATH, f"{avatar_id}_thumb.png")
    cv2.imwrite(thumbnail_path, cv2.cvtColor(thumbnail, cv2.COLOR_RGB2BGR))
    return thumbnail_path


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8002)
