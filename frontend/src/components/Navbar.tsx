import React, { useState } from "react";
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

  return (
    <nav className="sticky top-0 z-50 bg-white dark:bg-slate-900 border-b border-gray-100 dark:border-slate-800 relative transition-colors duration-200">
      <div className="max-w-2xl mx-auto px-4 min-h-14 flex flex-col md:flex-row md:items-center md:justify-between">
        <div className="flex items-center justify-between h-14 w-full md:w-auto">
          <span className="font-semibold text-gray-900 dark:text-white text-base">
            <a href="/" onClick={() => setIsOpen(false)}>
              SocialMedia
            </a>
          </span>

          {user && (
            <button
              onClick={() => setIsOpen(!isOpen)}
              className="md:hidden p-2 text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white text-xl focus:outline-none"
              aria-label="Toggle Menu"
            >
              {isOpen ? "✕" : "☰"}
            </button>
          )}
        </div>

        {!user ? (
          <div className="h-14 flex items-center">
            <button
              onClick={() => navigate("/login")}
              className="text-sm font-medium text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300"
            >
              Login
            </button>
          </div>
        ) : (
          <div
            className={`${
              isOpen ? "flex" : "hidden"
            } md:flex flex-col md:flex-row items-stretch md:items-center gap-2 md:gap-1 pb-4 md:pb-0 bg-white dark:bg-slate-900 left-0 w-full md:w-auto`}
          >
            {links.map(({ label, path, icon }) => {
              const active = location.pathname === path;
              return (
                <button
                  key={path}
                  onClick={() => handleNavigation(path)}
                  title={label}
                  className={`flex flex-row md:flex-col items-center justify-start md:justify-center gap-3 md:gap-0.5 px-4 md:px-3 py-2.5 md:py-1.5 rounded-lg text-sm md:text-xs transition-colors ${
                    active
                      ? "text-blue-600 bg-blue-50 dark:text-blue-400 dark:bg-slate-800"
                      : "text-gray-500 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 hover:bg-gray-50 dark:hover:bg-slate-800"
                  }`}
                >
                  <i className={`ti ${icon} text-lg`} />
                  <span>{label}</span>
                </button>
              );
            })}

            <button
              onClick={() => handleNavigation("/notifications")}
              title="Notifications"
              className={`relative flex flex-row md:flex-col items-center justify-start md:justify-center gap-3 md:gap-0.5 px-4 md:px-3 py-2.5 md:py-1.5 rounded-lg text-sm md:text-xs transition-colors ${
                location.pathname === "/notifications"
                  ? "text-blue-600 bg-blue-50 dark:text-blue-400 dark:bg-slate-800"
                  : "text-gray-500 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 hover:bg-gray-50 dark:hover:bg-slate-800"
              }`}
            >
              <div className="relative">
                <i className="ti ti-bell text-lg" />
                {unreadCount > 0 && (
                  <span className="absolute -top-1 -right-12.5 w-4 h-4 bg-red-500 text-white text-[10px] font-medium rounded-full flex items-center justify-center">
                    {unreadCount > 9 ? "9+" : unreadCount}
                  </span>
                )}
              </div>
              <span>Notifications</span>
            </button>

            <div className="hidden md:block w-px h-5 bg-gray-200 dark:bg-slate-700 mx-1" />

            <button
              onClick={() => {
                logout();
                setIsOpen(false);
              }}
              title="Logout"
              className="flex flex-row md:flex-col items-center justify-start md:justify-center gap-3 md:gap-0.5 px-4 md:px-3 py-2.5 md:py-1.5 rounded-lg text-sm md:text-xs text-gray-500 dark:text-gray-400 hover:text-red-500 dark:hover:text-red-400 hover:bg-red-50 dark:hover:bg-red-950/30 transition-colors"
            >
              <i className="ti ti-logout text-lg" />
              <span>Logout</span>
            </button>
          </div>
        )}
      </div>
    </nav>
  );
};

export default Navbar;
