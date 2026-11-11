// The access token lives only in memory, never in localStorage, so an XSS bug
// can't read a long-lived copy of it. After a page reload, the httpOnly
// refresh-token cookie is used to get a new one (see AuthProvider).
let accessToken: string | null = null;

export function getAccessToken(): string | null {
  return accessToken;
}

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

// Remove the token older versions of the app stored in localStorage.
try {
  localStorage.removeItem("token");
} catch {
  // storage unavailable — nothing to clean up
}
