<template>
  <div class="home">
    <div class="hero">
      <h1 class="hero-title">
        <span class="logo-at">@</span>potheca
      </h1>
      <p class="hero-subtitle">
        Your personal knowledge base. Notes, tasks, and everything in between —
        organised and always within reach.
      </p>
      <div class="hero-actions">
        <button class="cta-btn cta-btn--primary">Get Started</button>
        <button class="cta-btn cta-btn--ghost">Learn More</button>
      </div>
    </div>

    <div class="features">
      <div class="feature-card">
        <div class="feature-icon"><i class="pi pi-file-edit"></i></div>
        <h3>Notes</h3>
        <p>Capture ideas, write docs, and organise your thoughts with rich notes and folder structure.</p>
      </div>
      <div class="feature-card">
        <div class="feature-icon"><i class="pi pi-check-square"></i></div>
        <h3>Tasks</h3>
        <p>Track work across projects with a clean task list that keeps you focused on what matters.</p>
      </div>
      <div class="feature-card">
        <div class="feature-icon"><i class="pi pi-lock"></i></div>
        <h3>Private &amp; Secure</h3>
        <p>Your data is yours. Secured with industry-standard authentication and access controls.</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.home {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 5rem 2rem;
  gap: 4rem;
  overflow-y: auto;
}

.hero {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  max-width: 640px;
  gap: 1.25rem;
}

.hero-title {
  font-size: 3.5rem;
  font-weight: 800;
  margin: 0;
  letter-spacing: 0.04em;
  background: var(--gradient-brand);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.logo-at {
  font-style: italic;
}

.hero-subtitle {
  font-size: 1.1rem;
  color: var(--text-secondary);
  line-height: 1.7;
  margin: 0;
}

.hero-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 0.5rem;
}

.cta-btn {
  padding: 0.65rem 1.75rem;
  border-radius: 10px;
  font-size: 0.95rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.cta-btn--primary {
  background: var(--gradient-brand);
  border: none;
  color: #fff;
  box-shadow: 0 0 20px var(--glow-purple);
}

.cta-btn--primary:hover {
  box-shadow: 0 0 32px var(--glow-purple);
  opacity: 0.9;
}

.cta-btn--ghost {
  background: transparent;
  border: 1px solid var(--border-purple);
  color: var(--text-secondary);
}

.cta-btn--ghost:hover {
  background: var(--bg-hover);
  color: var(--text-primary);
}

.features {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 1.25rem;
  width: 100%;
  max-width: 860px;
}

.feature-card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 14px;
  padding: 1.75rem 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.feature-card:hover {
  border-color: var(--border-purple);
  box-shadow: 0 0 24px var(--glow-purple);
}

.feature-icon {
  font-size: 1.5rem;
  background: var(--gradient-brand);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.feature-card h3 {
  margin: 0;
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
}

.feature-card p {
  margin: 0;
  font-size: 0.875rem;
  color: var(--text-muted);
  line-height: 1.6;
}
</style>
