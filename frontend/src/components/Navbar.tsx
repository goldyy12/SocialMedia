import { useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "../context/useAuth";
import { useQuery } from "@tanstack/react-query";
import api from "../Api";

async function fetchUnreadCount(): Promise<number> {
  const response = await api.get("/notifications/unread-count");
  return response.data.count;
}

const Navbar = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuth();
  const [isOpen, setIsOpen] = useState<boolean>(false);

  const { data: unreadCount = 0 } = useQuery<number>({
    queryKey: ["unreadNotifications"],
    queryFn: fetchUnreadCount,
    refetchInterval: 30000,
    enabled: !!user,
  });

  const links = [
    { label: "Home", path: "/", icon: "ti-home" },
    { label: "Requests", path: "/followrequests", icon: "ti-user-plus" },
    { label: "Add Friends", path: "/addfriends", icon: "ti-users" },
    { label: "Profile", path: "/myprofile", icon: "ti-user-circle" },
    { label: "Messages", path: "/messages", icon: "ti-message" },
  ];

  const handleNavigation = (path: string) => {
    navigate(path);
    setIsOpen(false);
  };

  const tabClass = (active: boolean) =>
    `pill-tab w-full md:w-auto justify-start md:justify-center ${
      active
        ? "pill-tab-soft"
        : "border-transparent text-charcoal active:bg-surface-soft"
    }`;

  return (
    <nav className="sticky top-0 z-50 border-b border-hairline-soft bg-white">
      <div className="mx-auto flex min-h-16 max-w-5xl flex-col px-4 md:flex-row md:items-center md:justify-between">
        <div className="flex h-16 w-full items-center justify-between md:w-auto">
          <a
            href="/"
            onClick={() => setIsOpen(false)}
            className="text-xl font-medium tracking-tight text-ink-deep"
          >
            SocialMedia
          </a>

          {user && (
            <button
              onClick={() => setIsOpen(!isOpen)}
              className="flex h-11 w-11 items-center justify-center rounded-full border border-hairline text-lg text-ink md:hidden"
              aria-label="Toggle Menu"
            >
              {isOpen ? "✕" : "☰"}
            </button>
          )}
        </div>

        {!user ? (
          <div className="flex h-16 items-center">
            <button onClick={() => navigate("/login")} className="btn-primary">
              Log in
            </button>
          </div>
        ) : (
          <div
            className={`${
              isOpen ? "flex" : "hidden"
            } w-full flex-col items-stretch gap-2 pb-4 md:flex md:w-auto md:flex-row md:items-center md:gap-1 md:pb-0`}
          >
            {links.map(({ label, path, icon }) => (
              <button
                key={path}
                onClick={() => handleNavigation(path)}
                title={label}
                className={tabClass(location.pathname === path)}
              >
                <i className={`ti ${icon} text-lg`} />
                <span>{label}</span>
              </button>
            ))}

            <button
              onClick={() => handleNavigation("/notifications")}
              title="Notifications"
              className={tabClass(location.pathname === "/notifications")}
            >
              <i className="ti ti-bell text-lg" />
              <span>Notifications</span>
              {unreadCount > 0 && (
                <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-critical px-1.5 text-[11px] font-bold text-white">
                  {unreadCount > 9 ? "9+" : unreadCount}
                </span>
              )}
            </button>

            <div className="mx-1 hidden h-5 w-px bg-hairline-soft md:block" />

            <button
              onClick={() => {
                logout();
                setIsOpen(false);
              }}
              title="Log out"
              className="pill-tab w-full justify-start border-transparent text-steel active:bg-surface-soft md:w-auto md:justify-center"
            >
              <i className="ti ti-logout text-lg" />
              <span>Log out</span>
            </button>
          </div>
        )}
      </div>
    </nav>
  );
};

export default Navbar;
