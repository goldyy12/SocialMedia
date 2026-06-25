import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import AuthProvider from "./context/AuthProvider";
import ProtectedRoute from "./helpers/ProtectedRoutes";
import Home from "./pages/Home";
import Login from "./pages/Login";
import Register from "./pages/Register";
import Profile from "./pages/Profile.tsx";
import Layout from "./components/Layout";
import FollowRequests from "./pages/FollowRequests";
import AddFriends from "./pages/AddFriends";
import Following from "./pages/Following";
import Followers from "./pages/Followers";
import UserProfiles from "./pages/UserProfiles.tsx";
import Notifications from "./pages/Notifications.tsx";
import Messages from "./pages/Messages.tsx";

function App() {
  return (
    <AuthProvider>
      <Router>
        <Routes>
          <Route path="/" element={<Layout />}>
            {/* Protected routes */}
            <Route
              path="/"
              element={
                <ProtectedRoute>
                  <Home />
                </ProtectedRoute>
              }
            />
            <Route
              path="/myprofile"
              element={
                <ProtectedRoute>
                  <Profile />
                </ProtectedRoute>
              }
            />
            <Route
              path="/followrequests"
              element={
                <ProtectedRoute>
                  <FollowRequests />
                </ProtectedRoute>
              }
            />
            <Route
              path="/addfriends"
              element={
                <ProtectedRoute>
                  <AddFriends />
                </ProtectedRoute>
              }
            />
            <Route
              path="/following"
              element={
                <ProtectedRoute>
                  <Following />
                </ProtectedRoute>
              }
            />
            <Route
              path="/followers"
              element={
                <ProtectedRoute>
                  <Followers />
                </ProtectedRoute>
              }
            />
            <Route
              path="/user/:id"
              element={
                <ProtectedRoute>
                  <UserProfiles />
                </ProtectedRoute>
              }
            />
            <Route
              path="/notifications"
              element={
                <ProtectedRoute>
                  <Notifications />
                </ProtectedRoute>
              }
            />
            <Route
              path="/messages"
              element={
                <ProtectedRoute>
                  <Messages />
                </ProtectedRoute>
              }
            />
          </Route>

          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
        </Routes>
      </Router>
    </AuthProvider>
  );
}

export default App;
