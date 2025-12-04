// src/api/http.js
import axios from "axios";

const baseURL = (import.meta.env.VITE_API_URL || "http://localhost:5005/api").replace(/\/+$/, "");
const api = axios.create({
  baseURL,
  headers: { "Content-Type": "application/json" },
  timeout: 10000,
  withCredentials: true,
});

let onUnauthorized = null;
export function setOnUnauthorized(cb) { onUnauthorized = cb; }

// request interceptor — оставляем
api.interceptors.request.use(cfg => cfg, err => Promise.reject(err));

// response interceptor — глобальная обработка 401, но пропускаем если в конфиге стоит skipUnauthorizedHandler
api.interceptors.response.use(
  r => r,
  err => {
    const status = err?.response?.status;
    const skip = err?.config?.skipUnauthorizedHandler === true;
    if (status === 401 && !skip && typeof onUnauthorized === "function") {
      try { onUnauthorized(err); } catch (e) { /* swallow */ }
    }
    return Promise.reject(err);
  }
);

export async function http(url, { method = "GET", body, skipUnauthorizedHandler = false } = {}) {
  const cfg = { method, url, data: body ?? undefined, skipUnauthorizedHandler };
  const res = await api(cfg);
  return res.data;
}

export default api;
