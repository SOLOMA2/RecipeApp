import React, { useEffect, useMemo, useState } from "react";
import api from "../api/http";
import { useNavigate } from "react-router-dom";

const ICON_PRESETS = [
  { icon: "☀️", color: "#FDE68A", mood: "quick" },
  { icon: "🍲", color: "#FECACA", mood: "classic" },
  { icon: "🌙", color: "#C7D2FE", mood: "comfort" },
  { icon: "🧁", color: "#FBCFE8", mood: "sweet" },
  { icon: "🥗", color: "#BBF7D0", mood: "party" },
  { icon: "🥤", color: "#BAE6FD", mood: "fresh" }
];

const MOOD_FILTERS = [
  { value: "all", label: "Все" },
  { value: "quick", label: "Быстрые" },
  { value: "comfort", label: "Комфорт" },
  { value: "party", label: "Для компании" },
  { value: "fresh", label: "Полезные" },
  { value: "sweet", label: "Сладкие" }
];

export default function Categories() {
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [emptyMessage, setEmptyMessage] = useState("");
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState("all");
  const navigate = useNavigate();

  useEffect(() => {
    let mounted = true;
    (async () => {
      setLoading(true);
      try {
        setError("");
        setEmptyMessage("");
        // Загружаем категории с количеством рецептов
        const response = await api.get("/categories/with-counts?pageSize=100");
        if (!mounted) return;

        const payload = response?.data ?? response;
        const fromApi = Array.isArray(payload?.items)
          ? payload.items
          : Array.isArray(payload)
          ? payload
          : [];

        if (!fromApi.length) {
          setCategories([]);
          setEmptyMessage("Категории пока не созданы. Добавьте их через админку и привяжите рецепты.");
          return;
        }

        const hydrated = fromApi.map((category, index) => ({
          id: category.id ?? index,
          name: category.name ?? `Категория ${index + 1}`,
          description: category.description ?? "Описание категории появится позже",
          icon: ICON_PRESETS[index % ICON_PRESETS.length].icon,
          color: ICON_PRESETS[index % ICON_PRESETS.length].color,
          recipesCount: category.recipesCount ?? 0,
          mood: category.mood ?? ICON_PRESETS[index % ICON_PRESETS.length].mood
        }));

        setCategories(hydrated);
      } catch (err) {
        console.warn("Не удалось получить категории", err);
        if (mounted) {
          setError("Не удалось загрузить категории. Проверьте соединение или перезапустите API.");
          setCategories([]);
        }
      } finally {
        if (mounted) setLoading(false);
      }
    })();

    return () => {
      mounted = false;
    };
  }, []);

  const filteredCategories = useMemo(() => {
    return categories.filter((cat) => {
      const matchText =
        !search.trim() ||
        cat.name?.toLowerCase().includes(search.toLowerCase()) ||
        cat.description?.toLowerCase().includes(search.toLowerCase());

      const matchFilter = filter === "all" || cat.mood === filter;
      return matchText && matchFilter;
    });
  }, [categories, filter, search]);

  return (
    <div className="categories-page">
      <section className="categories-hero">
        <div className="site-container">
          <p className="eyebrow">Навигация по рецептам</p>
          <h1>Категории и подборки</h1>
          <p className="subtitle">
            Используйте фильтры, чтобы быстро найти подборку под настроение или задачу.
          </p>

          <div className="categories-search">
            <div className="search-input-wrapper categories-search-input">
              <span className="search-icon">🔎</span>
              <input
                type="search"
                placeholder="Поиск по названию или описанию"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
              />
            </div>

            <div className="filters">
              {MOOD_FILTERS.map((option) => (
                <button
                  key={option.value}
                  className={`filter-chip ${filter === option.value ? "active" : ""}`}
                  onClick={() => setFilter(option.value)}
                >
                  {option.label}
                </button>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section className="site-container categories-grid-section">
        <div className="categories-section-header">
          <div>
            <h2>Популярные категории</h2>
            <p className="section-subtitle">
              {filteredCategories.length} {filteredCategories.length === 1 ? "категория" : "категории"} доступно
            </p>
          </div>

          <button className="btn btn-ghost" onClick={() => navigate("/recipes/create")}>
            + Внести свой рецепт
          </button>
        </div>

        {error && <div className="inline-alert">{error}</div>}

        {loading ? (
          <div className="categories-skeleton">
            {Array.from({ length: 6 }).map((_, index) => (
              <div key={index} className="skeleton-card" />
            ))}
          </div>
        ) : filteredCategories.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">📂</div>
            <h3>Категории не найдены</h3>
            <p>{emptyMessage || "Создайте категорию и привяжите к ней рецепты."}</p>
          </div>
        ) : (
          <div className="categories-grid">
            {filteredCategories.map((category) => (
              <article key={category.id} className="category-card">
                <div className="category-icon" style={{ backgroundColor: category.color }}>
                  {category.icon}
                </div>

                <div className="category-body">
                  <div className="category-meta">
                    <span className="category-name">{category.name}</span>
                    <span className="category-count">
                      {category.recipesCount} {category.recipesCount === 1 ? "рецепт" : category.recipesCount < 5 ? "рецепта" : "рецептов"}
                    </span>
                  </div>
                  <p className="category-description">{category.description}</p>
                </div>

                <div className="category-footer">
                  <button
                    className="category-link"
                    disabled={!category.recipesCount}
                    onClick={() =>
                      navigate("/", { state: { categoryId: category.id, categoryName: category.name } })
                    }
                  >
                    {category.recipesCount > 0 ? `Смотреть ${category.recipesCount} рецептов →` : "Нет рецептов"}
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

