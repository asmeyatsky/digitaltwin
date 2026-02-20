import {
  getToken,
  setToken,
  getRefreshToken,
  setRefreshToken,
  getUser,
  setUser,
  clearToken,
  isAuthenticated,
} from "@/lib/auth";

beforeEach(() => {
  localStorage.clear();
  jest.clearAllMocks();
});

describe("auth token helpers", () => {
  test("getToken / setToken roundtrip", () => {
    setToken("abc123");
    expect(getToken()).toBe("abc123");
  });

  test("getRefreshToken / setRefreshToken roundtrip", () => {
    setRefreshToken("refresh-xyz");
    expect(getRefreshToken()).toBe("refresh-xyz");
  });

  test("setUser / getUser roundtrip", () => {
    const user = { id: "1", username: "alice", roles: ["admin"] };
    setUser(user);
    expect(getUser()).toEqual(user);
  });

  test("getUser returns null for invalid JSON", () => {
    localStorage.setItem("dt_user", "not-json");
    expect(getUser()).toBeNull();
  });

  test("getUser returns null when no user stored", () => {
    expect(getUser()).toBeNull();
  });

  test("clearToken removes all keys", () => {
    setToken("t");
    setRefreshToken("r");
    setUser({ id: "1", username: "u", roles: [] });
    clearToken();
    expect(getToken()).toBeNull();
    expect(getRefreshToken()).toBeNull();
    expect(getUser()).toBeNull();
  });

  test("isAuthenticated returns true with token", () => {
    setToken("some-token");
    expect(isAuthenticated()).toBe(true);
  });

  test("isAuthenticated returns false without token", () => {
    expect(isAuthenticated()).toBe(false);
  });
});
