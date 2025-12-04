// src/services/auth.js
import api from "../api/http";

// регистрация
export async function register(payload) {
  const res = await api.post("/Account/register", payload);
  return res.data;
}

// логин — сервер устанавливает httpOnly cookie; возвращаем user info
export async function login(payload) {
  const res = await api.post("/Account/login", payload);
  return res.data;
}

// получить текущего пользователя по cookie
export async function fetchMe() {
  // пометка skipUnauthorizedHandler: true предотвращает глобальный onUnauthorized
  const res = await api.get("/Account/me", { skipUnauthorizedHandler: true });
  return res.data;
}

// logout — вызывает backend чтобы удалить cookie
export async function logoutRequest() {
  try {
    const res = await api.post("/Account/logout");
    return res.data;
  } catch (e) {
    // ignore server errors
    return null;
  }
}

// noop для совместимости
export function saveToken() {}
export function removeToken() {}
export function getToken() { return null; }
export function applyTokenToApi() {}
