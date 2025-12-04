import React, { useEffect, useState, useMemo } from "react";
import { useNavigate, useParams, useLocation } from "react-router-dom";
import api from "../api/http";

export default function RecipeDetails() {
  const { id } = useParams();
  const navigate = useNavigate();
  const location = useLocation();

  const [recipe, setRecipe] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    (async () => {
      try {
        const res = await api.get(`/recipes/${id}`);
        if (!active) return;
        const data = res?.data ?? res;
        setRecipe(data);
      } catch (err) {
        console.error("Failed to load recipe", err);
        if (!active) return;
        setError("Не удалось загрузить рецепт. Попробуйте обновить страницу.");
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => {
      active = false;
    };
  }, [id]);

  const meta = useMemo(() => {
    if (!recipe) return [];
    return [
      recipe.cookingTimeMinutes ? `${recipe.cookingTimeMinutes} мин` : null,
      recipe.calories ? `${recipe.calories.toFixed(0)} ккал` : null,
      recipe.weight ? `${recipe.weight.toFixed(0)} г` : null,
    ].filter(Boolean);
  }, [recipe]);

  const nutritionPerRecipe = useMemo(() => {
    const n = recipe?.nutritionPerRecipe ?? {
      calories: recipe?.calories ?? 0,
      protein: recipe?.protein ?? 0,
      fat: recipe?.fat ?? 0,
      carbohydrates: recipe?.carbohydrates ?? 0,
    };
    return {
      calories: n.calories ?? 0,
      protein: n.protein ?? 0,
      fat: n.fat ?? 0,
      carbs: n.carbohydrates ?? 0,
    };
  }, [recipe]);

  const nutritionPer100 = useMemo(() => {
    const n = recipe?.nutritionPer100g ?? {
      calories: 0,
      protein: 0,
      fat: 0,
      carbohydrates: 0,
    };
    return {
      calories: n.calories ?? 0,
      protein: n.protein ?? 0,
      fat: n.fat ?? 0,
      carbs: n.carbohydrates ?? 0,
    };
  }, [recipe]);

  if (loading) {
    return (
      <div className="recipe-details-page">
        <div className="loading-state">
          <div className="loading-spinner"></div>
          <p>Загружаем рецепт...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="recipe-details-page">
        <div className="error-banner">
          <p>{error}</p>
          <button className="btn-primary" onClick={() => navigate(0)}>
            Обновить
          </button>
        </div>
      </div>
    );
  }

  if (!recipe) {
    return (
      <div className="recipe-details-page">
        <div className="empty-state">
          <div className="empty-icon">🤷</div>
          <h3>Рецепт не найден</h3>
          <button className="btn-primary" onClick={() => navigate("/")}>
            На главную
          </button>
        </div>
      </div>
    );
  }

  const fromCreateFlow = Boolean(location.state?.message);

  return (
    <div className="recipe-details-page">
      <div className="details-header">
        <button className="back-btn" onClick={() => navigate(-1)}>
          ← Назад
        </button>
        {fromCreateFlow && (
          <div className="success-banner">{location.state.message}</div>
        )}
      </div>

      <div className="details-hero">
        <div className="hero-content">
          <h1>{recipe.title}</h1>
          <p className="hero-description">{recipe.description}</p>
          <div className="hero-meta">
            {recipe.author?.userName && (
              <span>Автор: {recipe.author.userName}</span>
            )}
            {meta.map((item, index) => (
              <span key={index}>{item}</span>
            ))}
            {recipe.createdAt && (
              <span>
                Создан: {new Date(recipe.createdAt).toLocaleDateString("ru-RU")}
              </span>
            )}
          </div>
        </div>

        <div className="hero-image">
          {recipe.imageUrl ? (
            <img src={recipe.imageUrl} alt={recipe.title} />
          ) : (
            <div className="image-placeholder">
              <span role="img" aria-label="recipe">
                🍽️
              </span>
            </div>
          )}
        </div>
      </div>

      <div className="details-grid">
        <section className="details-section">
          <h2>Пищевая ценность</h2>
          <div className="details-rating">
            <span>Рейтинг: {nutritionPerRecipe ? recipe.rating?.toFixed(1) ?? "0.0" : "0.0"} / 5</span>
            <span>Оценок: {recipe.ratingCount ?? 0}</span>
            <span>Лайков: {recipe.likesCount ?? 0}</span>
          </div>
          <div className="nutrition-summary-grid">
            <div className="nutrition-card">
              <h3>На весь рецепт</h3>
              <p className="muted">Выход: {recipe.weight?.toFixed(0) || 0} г</p>
              <ul>
                <li><span>Калории</span><strong>{nutritionPerRecipe.calories.toFixed(1)}</strong></li>
                <li><span>Белки</span><strong>{nutritionPerRecipe.protein.toFixed(1)} г</strong></li>
                <li><span>Жиры</span><strong>{nutritionPerRecipe.fat.toFixed(1)} г</strong></li>
                <li><span>Углеводы</span><strong>{nutritionPerRecipe.carbs.toFixed(1)} г</strong></li>
              </ul>
            </div>
            <div className="nutrition-card">
              <h3>На 100 г</h3>
              <p className="muted">Автоматический расчёт</p>
              <ul>
                <li><span>Калории</span><strong>{nutritionPer100.calories.toFixed(1)}</strong></li>
                <li><span>Белки</span><strong>{nutritionPer100.protein.toFixed(1)} г</strong></li>
                <li><span>Жиры</span><strong>{nutritionPer100.fat.toFixed(1)} г</strong></li>
                <li><span>Углеводы</span><strong>{nutritionPer100.carbs.toFixed(1)} г</strong></li>
              </ul>
            </div>
          </div>
        </section>

        <section className="details-section">
          <h2>Ингредиенты</h2>
          {recipe.ingredients?.length ? (
            <ul className="ingredients-list detailed">
              {recipe.ingredients.map((ingredient) => (
                <li key={ingredient.id ?? ingredient.title}>
                  <span className="ingredient-title">{ingredient.title}</span>
                  <span className="ingredient-meta">
                    {[
                      ingredient.quantity ? `${ingredient.quantity}` : null,
                      ingredient.unit,
                      ingredient.weight ? `${ingredient.weight} г` : null,
                    ]
                      .filter(Boolean)
                      .join(" • ")}
                  </span>
                </li>
              ))}
            </ul>
          ) : (
            <p className="muted">Список ингредиентов не указан.</p>
          )}
        </section>

        <section className="details-section">
          <h2>Приготовление</h2>
          {recipe.cookingMethod ? (
            <p className="cooking-method">{recipe.cookingMethod}</p>
          ) : (
            <p className="muted">Метод приготовления не указан.</p>
          )}
        </section>

        <section className="details-section">
          <h2>Категории и теги</h2>
          <div className="tags-wrapper">
            {recipe.categories?.length ? (
              recipe.categories.map((category) => (
                <span key={category.id} className="tag chip">
                  {category.name}
                </span>
              ))
            ) : (
              <span className="muted">Категории не указаны.</span>
            )}
          </div>
          <div className="tags-wrapper">
            {recipe.tags?.length ? (
              recipe.tags.map((tag) => (
                <span key={tag.id ?? tag.title} className="tag">
                  #{tag.title}
                </span>
              ))
            ) : (
              <span className="muted">Теги не указаны.</span>
            )}
          </div>
        </section>
      </div>
    </div>
  );
}

