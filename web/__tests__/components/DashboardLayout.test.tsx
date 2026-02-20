import React from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import DashboardLayout from "@/components/DashboardLayout";

const mockLogout = jest.fn();
let mockChecked = true;
let mockUser: { username: string; roles: string[] } | null = {
  username: "testadmin",
  roles: ["admin"],
};

jest.mock("@/lib/auth", () => ({
  useAuth: () => ({
    checked: mockChecked,
    user: mockUser,
    logout: mockLogout,
  }),
}));

// Mock Sidebar to avoid complex rendering
jest.mock("@/components/Sidebar", () => {
  return function MockSidebar() {
    return <nav data-testid="sidebar" />;
  };
});

beforeEach(() => {
  jest.clearAllMocks();
  mockChecked = true;
  mockUser = { username: "testadmin", roles: ["admin"] };
});

describe("DashboardLayout", () => {
  test("shows Loading... when not checked", () => {
    mockChecked = false;
    render(<DashboardLayout>content</DashboardLayout>);
    expect(screen.getByText("Loading...")).toBeInTheDocument();
  });

  test("renders children when checked", () => {
    render(<DashboardLayout><div>My Page Content</div></DashboardLayout>);
    expect(screen.getByText("My Page Content")).toBeInTheDocument();
  });

  test("displays username", () => {
    render(<DashboardLayout>content</DashboardLayout>);
    expect(screen.getByText("testadmin")).toBeInTheDocument();
  });

  test("logout button calls logout", async () => {
    const user = userEvent.setup();
    render(<DashboardLayout>content</DashboardLayout>);
    const logoutBtn = screen.getByTitle("Sign out");
    await user.click(logoutBtn);
    expect(mockLogout).toHaveBeenCalled();
  });
});
