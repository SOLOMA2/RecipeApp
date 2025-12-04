import React from "react";

const curatedCollections = [
  {
    title: "Неделя здорового питания",
    description: "7 рецептов без сахара и лишнего жира",
    badge: "Wellness",
    level: "Средний"
  },
  {
    title: "Комфортные ужины",
    description: "Теплые блюда из одной кастрюли",
    badge: "Comfort",
    level: "Простой"
  },
  {
    title: "Для гостей за 30 минут",
    description: "Закуски и мини-десерты для вечеринок",
    badge: "Party",
    level: "Быстрый"
  }
];

export default function Collections() {
  return (
    <div className="site-container collections-page">
      <header className="collections-hero">
        <p className="eyebrow">Экспериментальная зона</p>
        <h1>Авторские подборки</h1>
        <p className="subtitle">
          Здесь скоро появятся тематические подборки и рекомендации от редакции. Пока что делимся прототипом и
          собираем идеи.
        </p>
      </header>

      <section className="collections-grid">
        {curatedCollections.map((item) => (
          <article key={item.title} className="collection-card">
            <span className="collection-badge">{item.badge}</span>
            <h3>{item.title}</h3>
            <p>{item.description}</p>
            <div className="collection-footer">
              <span className="collection-level">{item.level}</span>
              <button className="btn btn-ghost" disabled>
                В разработке
              </button>
            </div>
          </article>
        ))}
      </section>
    </div>
  );
}

