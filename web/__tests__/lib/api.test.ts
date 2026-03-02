import { fetchApi, login, getPendingReports, getCommunityGroups } from "@/lib/api";

// Mock auth module
jest.mock("@/lib/auth", () => ({
  getToken: jest.fn(() => "test-token"),
}));

const mockGetToken = jest.requireMock("@/lib/auth").getToken as jest.Mock;

beforeEach(() => {
  jest.clearAllMocks();
  global.fetch = jest.fn() as jest.Mock;
});

afterEach(() => {
  jest.restoreAllMocks();
});

function mockFetch() {
  return global.fetch as jest.Mock;
}

describe("fetchApi", () => {
  test("adds Bearer auth header", async () => {
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ success: true, data: {} }),
    });

    await fetchApi("/test");
    const [, opts] = mockFetch().mock.calls[0];
    expect(opts.headers["Authorization"]).toBe("Bearer test-token");
  });

  test("adds Content-Type header", async () => {
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ success: true, data: {} }),
    });

    await fetchApi("/test");
    const [, opts] = mockFetch().mock.calls[0];
    expect(opts.headers["Content-Type"]).toBe("application/json");
  });

  test("constructs full URL from BASE_URL", async () => {
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ success: true, data: {} }),
    });

    await fetchApi("/api/test");
    const [url] = mockFetch().mock.calls[0];
    expect(url).toContain("/api/test");
  });

  test("returns parsed JSON response", async () => {
    const payload = { success: true, data: { id: 1 } };
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve(payload),
    });

    const result = await fetchApi("/test");
    expect(result).toEqual(payload);
  });

  test("throws on 401", async () => {
    mockFetch().mockResolvedValueOnce({
      ok: false,
      status: 401,
      json: () => Promise.resolve({}),
    });

    await expect(fetchApi("/test")).rejects.toThrow("Unauthorized");
  });

  test("throws with message on other errors", async () => {
    mockFetch().mockResolvedValueOnce({
      ok: false,
      status: 500,
      json: () => Promise.resolve({ message: "Server error" }),
    });

    await expect(fetchApi("/test")).rejects.toThrow("Server error");
  });

  test("no auth header when no token", async () => {
    mockGetToken.mockReturnValueOnce(null);

    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ success: true, data: {} }),
    });

    await fetchApi("/test");
    const [, opts] = mockFetch().mock.calls[0];
    expect(opts.headers["Authorization"]).toBeUndefined();
  });
});

describe("login", () => {
  test("sends correct payload", async () => {
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () =>
        Promise.resolve({
          success: true,
          token: "tok",
          refreshToken: "ref",
          expiresIn: 3600,
          user: { userId: "1", username: "admin", roles: ["admin"] },
        }),
    });

    await login("admin", "pass123");
    const [, opts] = mockFetch().mock.calls[0];
    const body = JSON.parse(opts.body);
    expect(body.Username).toBe("admin");
    expect(body.Password).toBe("pass123");
  });

  test("normalizes LoginResponseRaw keys", async () => {
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () =>
        Promise.resolve({
          success: true,
          token: "tok",
          refreshToken: "ref",
          expiresIn: 3600,
          user: { userId: "u1", username: "alice", roles: ["user"] },
        }),
    });

    const result = await login("alice", "pw");
    expect(result.token).toBe("tok");
    expect(result.refreshToken).toBe("ref");
    expect(result.user.id).toBe("u1");
    expect(result.user.username).toBe("alice");
  });

  test("throws on failure", async () => {
    mockFetch().mockResolvedValueOnce({
      ok: false,
      status: 401,
      json: () => Promise.resolve({ success: false, message: "Bad credentials" }),
    });

    await expect(login("admin", "wrong")).rejects.toThrow("Bad credentials");
  });
});

describe("getPendingReports", () => {
  test("passes pagination params", async () => {
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ success: true, data: { reports: [] } }),
    });

    await getPendingReports(2, 10);
    const [url] = mockFetch().mock.calls[0];
    expect(url).toContain("page=2");
    expect(url).toContain("pageSize=10");
  });
});

describe("getCommunityGroups", () => {
  test("passes query params", async () => {
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ success: true, data: { groups: [] } }),
    });

    await getCommunityGroups(1, 20, "Support", "search-term");
    const [url] = mockFetch().mock.calls[0];
    expect(url).toContain("category=Support");
    expect(url).toContain("search=search-term");
  });
});
