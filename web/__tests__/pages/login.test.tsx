import React from "react";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import LoginPage from "@/app/login/page";

// Mock api and auth
const mockLogin = jest.fn();
const mockSetToken = jest.fn();
const mockSetRefreshToken = jest.fn();
const mockSetUser = jest.fn();
const mockIsAuthenticated = jest.fn(() => false);
const mockReplace = jest.fn();

jest.mock("@/lib/api", () => ({
  login: (...args: unknown[]) => mockLogin(...args),
}));

jest.mock("@/lib/auth", () => ({
  setToken: (...args: unknown[]) => mockSetToken(...args),
  setRefreshToken: (...args: unknown[]) => mockSetRefreshToken(...args),
  setUser: (...args: unknown[]) => mockSetUser(...args),
  isAuthenticated: () => mockIsAuthenticated(),
}));

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    replace: mockReplace,
    push: jest.fn(),
    back: jest.fn(),
    prefetch: jest.fn(),
  }),
}));

beforeEach(() => {
  jest.clearAllMocks();
  mockIsAuthenticated.mockReturnValue(false);
});

describe("LoginPage", () => {
  test("renders form fields", () => {
    render(<LoginPage />);
    expect(screen.getByLabelText(/username/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
  });

  test("renders heading text", () => {
    render(<LoginPage />);
    expect(screen.getByText("Digital Twin Dashboard")).toBeInTheDocument();
  });

  test("submit disabled when fields empty", () => {
    render(<LoginPage />);
    const button = screen.getByRole("button", { name: /sign in/i });
    expect(button).toBeDisabled();
  });

  test("submit enabled when fields filled", async () => {
    const user = userEvent.setup();
    render(<LoginPage />);
    await user.type(screen.getByLabelText(/username/i), "admin");
    await user.type(screen.getByLabelText(/password/i), "pass123");
    const button = screen.getByRole("button", { name: /sign in/i });
    expect(button).toBeEnabled();
  });

  test("shows error on failed login", async () => {
    mockLogin.mockRejectedValueOnce(new Error("Invalid credentials"));
    const user = userEvent.setup();
    render(<LoginPage />);
    await user.type(screen.getByLabelText(/username/i), "admin");
    await user.type(screen.getByLabelText(/password/i), "wrong");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(screen.getByText("Invalid credentials")).toBeInTheDocument();
    });
  });

  test("success stores token and redirects", async () => {
    mockLogin.mockResolvedValueOnce({
      token: "tok",
      refreshToken: "ref",
      user: { id: "1", username: "admin", roles: ["admin"] },
    });

    const user = userEvent.setup();
    render(<LoginPage />);
    await user.type(screen.getByLabelText(/username/i), "admin");
    await user.type(screen.getByLabelText(/password/i), "pass123");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(mockSetToken).toHaveBeenCalledWith("tok");
      expect(mockSetRefreshToken).toHaveBeenCalledWith("ref");
      expect(mockSetUser).toHaveBeenCalledWith({
        id: "1",
        username: "admin",
        roles: ["admin"],
      });
      expect(mockReplace).toHaveBeenCalledWith("/chat");
    });
  });
});
