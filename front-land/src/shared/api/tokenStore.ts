// In-memory token store — access token nikad ne ide u sessionStorage/localStorage
// jer je dostupan XSS napadima. Token živi samo u memoriji procesa.
// Na page reload, httpOnly refresh cookie automatski vraća novu sesiju.

let _accessToken: string | null = null;

export function getAccessToken(): string | null {
  return _accessToken;
}

export function setAccessToken(token: string | null): void {
  _accessToken = token;
}
