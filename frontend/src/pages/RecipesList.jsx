import React, { useEffect, useState, useContext } from "react";
import api from "../api/http";
import { AuthContext } from "../context/AuthContext";
import { useNavigate, useLocation } from "react-router-dom";

function RecipeCard({ recipe, onOpen, onSave }) {
  return (
    <article className="recipe-card" onClick={() => onOpen(recipe.id)}>
      <div className="card-image">
        {recipe.imageUrl ? (
          <img src={recipe.imageUrl} alt={recipe.title} loading="lazy" />
        ) : (
          <div className="image-placeholder">
            <span>🍳</span>
          </div>
        )}
        <button 
          className="save-btn" 
          onClick={(e) => { e.stopPropagation(); onSave && onSave(recipe.id); }}
          aria-label="Поставить лайк"
        >
          ♡ {recipe.likesCount ?? 0}
        </button>
      </div>
      
      <div className="card-content">
        <h3 className="recipe-title">{recipe.title}</h3>
        <p className="recipe-description">{recipe.description}</p>
        
        <div className="recipe-meta">
          <span className="author">{recipe.authorName ?? recipe.userName}</span>
          <div className="rating">
            {[1,2,3,4,5].map(star => (
              <span 
                key={star} 
                className={star <= Math.round(recipe.rating || 0) ? "filled" : ""}
                onClick={(e) => { e.stopPropagation(); recipe.onRate && recipe.onRate(star); }}
              >
                ★
              </span>
            ))}
            <span className="rating-value">
              {recipe.rating ? recipe.rating.toFixed(1) : "0.0"}
            </span>
          </div>
        </div>
        
        <button 
          className="view-recipe-btn"
          onClick={(e) => { e.stopPropagation(); onOpen(recipe.id); }}
        >
          Смотреть рецепт
        </button>
      </div>
    </article>
  );
}

export default function RecipesList() {
  const [recipes, setRecipes] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");
  const [categoryId, setCategoryId] = useState(null);
  const [categoryName, setCategoryName] = useState("");
  const { isAuthenticated } = useContext(AuthContext);
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    // Обрабатываем переход со страницы категорий
    if (location.state?.categoryId) {
      setCategoryId(location.state.categoryId);
      setCategoryName(location.state.categoryName || "");
      navigate(location.pathname, { replace: true, state: null });
    }
  }, [location, navigate]);

  useEffect(() => {
    let mounted = true;
    (async () => {
      setLoading(true);
      try {
        // Формируем URL с параметрами
        const params = new URLSearchParams();
        if (categoryId) {
          params.append("categoryId", categoryId.toString());
        }
        if (searchQuery.trim()) {
          params.append("search", searchQuery.trim());
        }
        
        const url = `/recipes${params.toString() ? `?${params.toString()}` : ""}`;
        const res = await api.get(url);
        if (!mounted) return;
        const data = res?.data ?? res;
        const items = Array.isArray(data?.items)
          ? data.items
          : Array.isArray(data)
          ? data
          : [];
        setRecipes(items);
        setTotalCount(data?.totalCount ?? items.length);
      } catch (e) {
        console.error("Failed to load recipes:", e);
      } finally {
        if (mounted) setLoading(false);
      }
    })();
    return () => { mounted = false; };
  }, [categoryId, searchQuery]);

  const filteredRecipes = recipes;

  const onSaveRecipe = async (id) => {
    try {
      const res = await api.post(`/recipes/${id}/like`);
      const payload = res?.data ?? res;
      setRecipes(prev => prev.map(r => r.id === id ? { ...r, likesCount: payload.likesCount } : r));
    } catch (e) {
      console.error("Failed to like recipe:", e);
    }
  };

  const onRateRecipe = async (id, value) => {
    try {
      const res = await api.post(`/recipes/${id}/rate`, { value });
      const payload = res?.data ?? res;
      setRecipes(prev => prev.map(r => 
        r.id === id ? { ...r, rating: payload.rating, ratingCount: payload.ratingCount } : r
      ));
    } catch (e) {
      console.error("Failed to rate recipe:", e);
    }
  };

  if (loading) {
    return (
      <div className="loading-state">
        <div className="loading-spinner"></div>
        <p>Загружаем рецепты...</p>
      </div>
    );
  }

  return (
    <div className="recipes-page">
      {/* Hero Section */}
      <section className="recipes-hero">
        <div className="hero-content">
          <h1>Найдите свой идеальный рецепт</h1>
          <p>Тысячи проверенных рецептов от домашних поваров</p>
          
          <div className="search-container">
            <div className="search-input-wrapper">
              <span className="search-icon">🔍</span>
              <input
                type="text"
                placeholder="Поиск рецептов, ингредиентов..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="search-input"
              />
            </div>
          </div>

          {isAuthenticated && (
            <button 
              className="create-recipe-btn"
              onClick={() => navigate("/recipes/create")}
            >
              + Создать рецепт
            </button>
          )}
        </div>
      </section>

      {/* Recipes Grid */}
      <section className="recipes-section">
        <div className="section-header">
          <h2>
            {categoryName ? `Рецепты: ${categoryName}` : "Все рецепты"}
            {categoryId && (
              <button 
                className="btn btn-ghost" 
                onClick={() => {
                  setCategoryId(null);
                  setCategoryName("");
                  setSearchQuery("");
                }}
                style={{ marginLeft: "1rem", fontSize: "0.875rem" }}
              >
                ✕ Сбросить фильтр
              </button>
            )}
          </h2>
          <span className="recipes-count">
            {totalCount} {totalCount === 1 ? "рецепт" : totalCount < 5 ? "рецепта" : "рецептов"}
          </span>
        </div>

        {filteredRecipes.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">📝</div>
            <h3>Рецепты не найдены</h3>
            <p>Попробуйте изменить поисковый запрос</p>
          </div>
        ) : (
          <div className="recipes-grid">
            {filteredRecipes.map(recipe => (
              <RecipeCard
                key={recipe.id}
                recipe={{ 
                  ...recipe, 
                  onRate: (value) => onRateRecipe(recipe.id, value) 
                }}
                onOpen={(id) => navigate(`/recipes/${id}`)}
                onSave={onSaveRecipe}
              />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}