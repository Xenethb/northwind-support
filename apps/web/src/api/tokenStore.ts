
let accessToken: string | null = null;
let expiresAtUtc: string | null = null;

export function setAccessToken(
  token: string,
  expiresAt: string,
): void {
  accessToken = token;
  expiresAtUtc = expiresAt;
}

export function getAccessToken(): string | null {
  if (!accessToken || !expiresAtUtc) {
    return null;
  }

  const expiresAt = Date.parse(expiresAtUtc);

  if (!Number.isFinite(expiresAt) || Date.now() >= expiresAt) {
    clearAccessToken();
    return null;
  }

  return accessToken;
}

export function clearAccessToken(): void {
  accessToken = null;
  expiresAtUtc = null;
}
