import { Platform } from "react-native";

export interface BiometricData {
  type: string;
  value: number;
  unit: string;
  source: string;
  timestamp: string;
}

export async function fetchHealthKitData(): Promise<BiometricData[]> {
  if (Platform.OS !== "ios") return [];

  try {
    // Dynamic import for react-native-health (iOS only)
    const AppleHealthKit = await import("react-native-health").then(
      (m) => m.default
    );

    const permissions = {
      permissions: {
        read: ["HeartRate", "HeartRateVariability", "StepCount", "SleepAnalysis"],
      },
    };

    return new Promise((resolve) => {
      AppleHealthKit.initHealthKit(permissions, (err: any) => {
        if (err) {
          console.warn("HealthKit init failed:", err);
          resolve([]);
          return;
        }

        const results: BiometricData[] = [];
        const now = new Date();
        const oneDayAgo = new Date(now.getTime() - 24 * 60 * 60 * 1000);

        const options = {
          startDate: oneDayAgo.toISOString(),
          endDate: now.toISOString(),
          limit: 10,
        };

        // Heart Rate
        AppleHealthKit.getHeartRateSamples(options, (err: any, data: any[]) => {
          if (!err && data?.length) {
            const latest = data[data.length - 1];
            results.push({
              type: "heart_rate",
              value: latest.value,
              unit: "bpm",
              source: "apple_health",
              timestamp: latest.endDate,
            });
          }

          // HRV
          AppleHealthKit.getHeartRateVariabilitySamples(
            options,
            (err: any, data: any[]) => {
              if (!err && data?.length) {
                const latest = data[data.length - 1];
                results.push({
                  type: "hrv",
                  value: latest.value,
                  unit: "ms",
                  source: "apple_health",
                  timestamp: latest.endDate,
                });
              }

              // Steps
              AppleHealthKit.getStepCount(options, (err: any, data: any) => {
                if (!err && data) {
                  results.push({
                    type: "steps",
                    value: data.value,
                    unit: "count",
                    source: "apple_health",
                    timestamp: now.toISOString(),
                  });
                }

                resolve(results);
              });
            }
          );
        });
      });
    });
  } catch (err) {
    console.warn("HealthKit not available:", err);
    return [];
  }
}

// Google Fit data source types
const GOOGLE_FIT_SCOPES = [
  "https://www.googleapis.com/auth/fitness.heart_rate.read",
  "https://www.googleapis.com/auth/fitness.activity.read",
  "https://www.googleapis.com/auth/fitness.sleep.read",
];

const GOOGLE_FIT_API = "https://www.googleapis.com/fitness/v1/users/me";

interface GoogleFitDataPoint {
  startTimeNanos: string;
  endTimeNanos: string;
  value: { fpVal?: number; intVal?: number }[];
}

interface GoogleFitDataset {
  point: GoogleFitDataPoint[];
}

async function googleFitRequest(
  accessToken: string,
  url: string
): Promise<any> {
  const response = await fetch(url, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  if (!response.ok) {
    throw new Error(`Google Fit API error: ${response.status}`);
  }
  return response.json();
}

async function getGoogleFitAccessToken(): Promise<string | null> {
  try {
    const { GoogleSignin } = await import(
      "@react-native-google-signin/google-signin"
    );

    GoogleSignin.configure({
      scopes: GOOGLE_FIT_SCOPES,
    });

    const isSignedIn = await GoogleSignin.isSignedIn();
    if (!isSignedIn) {
      await GoogleSignin.signIn();
    }

    const tokens = await GoogleSignin.getTokens();
    return tokens.accessToken;
  } catch (err) {
    console.warn("Google Sign-In failed:", err);
    return null;
  }
}

export async function fetchGoogleFitData(): Promise<BiometricData[]> {
  if (Platform.OS !== "android") return [];

  try {
    const accessToken = await getGoogleFitAccessToken();
    if (!accessToken) return [];

    const results: BiometricData[] = [];
    const now = Date.now();
    const oneDayAgo = now - 24 * 60 * 60 * 1000;
    const startNanos = oneDayAgo * 1_000_000;
    const endNanos = now * 1_000_000;

    // Heart Rate — derived:com.google.heart_rate.bpm:com.google.android.gms:merge_heart_rate_bpm
    try {
      const hrDataSource =
        "derived:com.google.heart_rate.bpm:com.google.android.gms:merge_heart_rate_bpm";
      const hrUrl = `${GOOGLE_FIT_API}/dataSources/${encodeURIComponent(hrDataSource)}/datasets/${startNanos}-${endNanos}`;
      const hrData: GoogleFitDataset = await googleFitRequest(
        accessToken,
        hrUrl
      );

      if (hrData.point?.length) {
        const latest = hrData.point[hrData.point.length - 1];
        const value = latest.value[0]?.fpVal ?? 0;
        results.push({
          type: "heart_rate",
          value: Math.round(value),
          unit: "bpm",
          source: "google_fit",
          timestamp: new Date(
            parseInt(latest.endTimeNanos) / 1_000_000
          ).toISOString(),
        });
      }
    } catch (err) {
      console.warn("Google Fit heart rate fetch failed:", err);
    }

    // Steps — derived:com.google.step_count.delta:com.google.android.gms:estimated_steps
    try {
      const body = {
        aggregateBy: [
          { dataTypeName: "com.google.step_count.delta" },
        ],
        bucketByTime: { durationMillis: 86400000 },
        startTimeMillis: oneDayAgo,
        endTimeMillis: now,
      };

      const response = await fetch(
        `${GOOGLE_FIT_API}/dataset:aggregate`,
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${accessToken}`,
            "Content-Type": "application/json",
          },
          body: JSON.stringify(body),
        }
      );

      if (response.ok) {
        const data = await response.json();
        const bucket = data.bucket?.[0];
        const dataset = bucket?.dataset?.[0];
        const point = dataset?.point?.[0];
        if (point) {
          const steps = point.value[0]?.intVal ?? 0;
          results.push({
            type: "steps",
            value: steps,
            unit: "count",
            source: "google_fit",
            timestamp: new Date().toISOString(),
          });
        }
      }
    } catch (err) {
      console.warn("Google Fit steps fetch failed:", err);
    }

    // Sleep — aggregate sleep segments from the last day
    try {
      const body = {
        aggregateBy: [
          { dataTypeName: "com.google.sleep.segment" },
        ],
        bucketByTime: { durationMillis: 86400000 },
        startTimeMillis: oneDayAgo,
        endTimeMillis: now,
      };

      const response = await fetch(
        `${GOOGLE_FIT_API}/dataset:aggregate`,
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${accessToken}`,
            "Content-Type": "application/json",
          },
          body: JSON.stringify(body),
        }
      );

      if (response.ok) {
        const data = await response.json();
        const bucket = data.bucket?.[0];
        const dataset = bucket?.dataset?.[0];
        if (dataset?.point?.length) {
          // Calculate total sleep minutes
          let totalSleepMs = 0;
          for (const point of dataset.point) {
            const start = parseInt(point.startTimeNanos) / 1_000_000;
            const end = parseInt(point.endTimeNanos) / 1_000_000;
            totalSleepMs += end - start;
          }
          const sleepHours = totalSleepMs / (1000 * 60 * 60);
          // Convert to quality score (0-100): 8 hours = 100, scale linearly
          const quality = Math.min(100, Math.round((sleepHours / 8) * 100));
          results.push({
            type: "sleep_quality",
            value: quality,
            unit: "score",
            source: "google_fit",
            timestamp: new Date().toISOString(),
          });
        }
      }
    } catch (err) {
      console.warn("Google Fit sleep fetch failed:", err);
    }

    return results;
  } catch (err) {
    console.warn("Google Fit integration error:", err);
    return [];
  }
}

export async function fetchBiometricData(): Promise<BiometricData[]> {
  if (Platform.OS === "ios") {
    return fetchHealthKitData();
  } else if (Platform.OS === "android") {
    return fetchGoogleFitData();
  }
  return [];
}
