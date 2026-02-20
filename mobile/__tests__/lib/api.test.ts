import { useAuthStore } from "@/lib/store";
import * as api from "@/lib/api";

beforeEach(() => {
  jest.clearAllMocks();
  (global.fetch as jest.Mock).mockReset();

  // Reset auth store
  useAuthStore.setState({
    token: "test-token",
    refreshToken: "test-refresh",
    user: null,
    isAuthenticated: true,
    isLoading: false,
  });
});

const mockFetch = () => global.fetch as jest.Mock;

describe("request (via startConversation)", () => {
  test("adds auth header", async () => {
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ success: true, data: { sessionId: "s1" } }),
    });

    await api.startConversation("hello");
    const [, opts] = mockFetch().mock.calls[0];
    expect(opts.headers["Authorization"]).toBe("Bearer test-token");
  });

  test("adds content-type header", async () => {
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ success: true, data: {} }),
    });

    await api.startConversation("hello");
    const [, opts] = mockFetch().mock.calls[0];
    expect(opts.headers["Content-Type"]).toBe("application/json");
  });

  test("returns parsed response", async () => {
    const payload = { success: true, data: { sessionId: "s1", response: "Hi!" } };
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve(payload),
    });

    const result = await api.startConversation("hello");
    expect(result).toEqual(payload);
  });

  test("401 triggers silentRefresh and retry", async () => {
    // First call returns 401
    mockFetch().mockResolvedValueOnce({
      ok: false,
      status: 401,
      json: () => Promise.resolve({}),
    });
    // Refresh call succeeds
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () =>
        Promise.resolve({
          success: true,
          token: "new-token",
          refreshToken: "new-refresh",
          expiresIn: 3600,
        }),
    });
    // Retry succeeds
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ success: true, data: { sessionId: "s2" } }),
    });

    const result = await api.startConversation("hello");
    expect(result.success).toBe(true);
    // 3 calls: original, refresh, retry
    expect(mockFetch()).toHaveBeenCalledTimes(3);
  });

  test("401 refresh-fail calls logout", async () => {
    const logoutSpy = jest.spyOn(useAuthStore.getState(), "logout");

    mockFetch().mockResolvedValueOnce({
      ok: false,
      status: 401,
      json: () => Promise.resolve({}),
    });
    // Refresh fails
    mockFetch().mockResolvedValueOnce({
      ok: false,
      status: 401,
      json: () => Promise.resolve({ success: false }),
    });

    await expect(api.startConversation("hello")).rejects.toThrow("Session expired");
    expect(logoutSpy).toHaveBeenCalled();
    logoutSpy.mockRestore();
  });
});

describe("login", () => {
  test("sends correct body", async () => {
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () =>
        Promise.resolve({
          success: true,
          token: "t",
          refreshToken: "r",
          expiresIn: 3600,
          user: { userId: "u1", username: "alice", roles: ["user"] },
        }),
    });

    await api.login("alice", "pass123");
    const [, opts] = mockFetch().mock.calls[0];
    const body = JSON.parse(opts.body);
    expect(body.Username).toBe("alice");
    expect(body.Password).toBe("pass123");
  });

  test("maps response correctly", async () => {
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

    const result = await api.login("alice", "pw");
    expect(result.data.accessToken).toBe("tok");
    expect(result.data.refreshToken).toBe("ref");
    expect(result.data.user.id).toBe("u1");
  });
});

describe("register", () => {
  test("maps fields correctly", async () => {
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () =>
        Promise.resolve({
          success: true,
          token: "t",
          refreshToken: "r",
          user: { userId: "u2", username: "bob@email.com", roles: ["user"] },
        }),
    });

    await api.register({
      Username: "bob@email.com",
      Email: "bob@email.com",
      Password: "pw",
      FirstName: "Bob",
      LastName: "Smith",
    });

    const [, opts] = mockFetch().mock.calls[0];
    const body = JSON.parse(opts.body);
    expect(body.Email).toBe("bob@email.com");
    expect(body.FirstName).toBe("Bob");
  });
});

describe("startConversation", () => {
  test("sends to correct path", async () => {
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ success: true, data: {} }),
    });

    await api.startConversation("hello");
    const [url] = mockFetch().mock.calls[0];
    expect(url).toContain("/api/conversation/start");
  });
});

describe("sendMessage", () => {
  test("sends to correct path", async () => {
    mockFetch().mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ success: true, data: {} }),
    });

    await api.sendMessage("conv-1", "hi there");
    const [url, opts] = mockFetch().mock.calls[0];
    expect(url).toContain("/api/conversation/message");
    const body = JSON.parse(opts.body);
    expect(body.conversationId).toBe("conv-1");
    expect(body.message).toBe("hi there");
  });
});
