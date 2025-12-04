// src/pages/AdminPanel.jsx
import React, { useEffect, useState, useContext } from "react";
import { AuthContext } from "../context/AuthContext";
import api from "../api/http";

export default function AdminPanelPage() {
  const { user } = useContext(AuthContext);
  const [stats, setStats] = useState(null);
  const [users, setUsers] = useState([]);
  const [err, setErr] = useState(null);
  const [loading, setLoading] = useState(true);
  const [lastUpdated, setLastUpdated] = useState(null);

  const fetchAll = async () => {
    try {
      setErr(null);
      setLoading(true);
      const s = await api.get("/admin/stats");
      setStats(s.data ?? s);
      const u = await api.get("/admin/users");
      setUsers(u.data ?? u);
      setLastUpdated(new Date().toISOString());
    } catch (e) {
      setErr(e?.response?.data?.title || e?.response?.data || e.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchAll(); }, []);

  const addRole = async (userId, role) => {
    if (!confirm(`Добавить роль ${role} пользователю?`)) return;
    try {
      await api.post("/admin/users/add-role", { userId, role });
      await fetchAll();
    } catch (e) { alert("Ошибка: " + (e?.response?.data || e.message)); }
  };

  const removeRole = async (userId, role) => {
    if (!confirm(`Убрать роль ${role} у пользователя?`)) return;
    try {
      await api.post("/admin/users/remove-role", { userId, role });
      await fetchAll();
    } catch (e) { alert("Ошибка: " + (e?.response?.data || e.message)); }
  };

  const deleteUser = async (userId) => {
    if (!confirm("Удалить пользователя? Это действие необратимо.")) return;
    try {
      await api.delete(`/admin/users/${userId}`);
      await fetchAll();
    } catch (e) { alert("Ошибка: " + (e?.response?.data || e.message)); }
  };

  const statCards = [
    { label: "Пользователи", value: stats?.users ?? "—", icon: "👥" },
    { label: "Рецепты", value: stats?.recipes ?? "—", icon: "📚" },
    { label: "Время сервера", value: stats?.serverTime ?? "—", icon: "⏱️" }
  ];

  const lastSync = lastUpdated
    ? new Date(lastUpdated).toLocaleTimeString("ru-RU")
    : "—";

  return (
      <div className="admin-page">
        <section className="admin-hero">
          <div>
            <p className="muted">Панель администратора</p>
            <h1>Контроль платформы RecipeBook</h1>
            <p>Отслеживайте ключевые метрики и управляйте пользователями в одном месте.</p>
            <div className="admin-hero-meta">
              <span>{user?.email}</span>
              <span>Роль: {(user?.roles || []).join(", ")}</span>
              <span>Пользователей: {stats?.users ?? "—"}</span>
            </div>
          </div>
          <div className="admin-hero-actions">
            <button className="btn btn-primary" onClick={fetchAll} disabled={loading}>
              {loading ? "Обновляем..." : "Обновить данные"}
            </button>
            <span className="muted">Последнее обновление: {lastSync}</span>
          </div>
        </section>

        {err && <div className="error-banner">{err}</div>}

        <section className="admin-stats-grid">
          {statCards.map(card => (
            <article className="admin-stat-card" key={card.label}>
              <span className="stat-icon" aria-hidden>{card.icon}</span>
              <div>
                <div className="stat-label">{card.label}</div>
                <div className="stat-value">{card.value}</div>
              </div>
            </article>
          ))}
        </section>

        <section className="admin-users-card">
          <header>
            <div>
              <h2>Пользователи</h2>
              <p className="muted">{users.length} записей</p>
            </div>
            <button className="btn btn-ghost" onClick={fetchAll} disabled={loading}>
              Обновить список
            </button>
          </header>

          {loading ? (
            <div className="loading-state small">
              <div className="loading-spinner"></div>
              <p>Загружаем пользователей...</p>
            </div>
          ) : users.length === 0 ? (
            <div className="admin-empty">
              <p>Пользователи не найдены.</p>
            </div>
          ) : (
            <div className="admin-table-wrapper">
              <table className="admin-table">
                <thead>
                  <tr>
                    <th>Email</th>
                    <th>Имя</th>
                    <th>Роли</th>
                    <th align="right">Действия</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map(u => {
                    const roles = u.roles || [];
                    const isAdmin = roles.includes("Admin");
                    return (
                      <tr key={u.id}>
                        <td>{u.email}</td>
                        <td>{u.userName}</td>
                        <td>
                          {roles.length ? roles.map(r => (
                            <span key={r} className="admin-role-badge">{r}</span>
                          )) : <span className="muted">нет ролей</span>}
                        </td>
                        <td>
                          <div className="admin-row-actions">
                            {isAdmin ? (
                              <button
                                className="admin-action-btn demote"
                                onClick={() => removeRole(u.id, "Admin")}
                              >
                                Убрать Admin
                              </button>
                            ) : (
                              <button
                                className="admin-action-btn promote"
                                onClick={() => addRole(u.id, "Admin")}
                              >
                                Сделать Admin
                              </button>
                            )}
                            <button
                              className="admin-action-btn danger"
                              onClick={() => deleteUser(u.id)}
                            >
                              Удалить
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </div>
  );
}
