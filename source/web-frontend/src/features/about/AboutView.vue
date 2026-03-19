<template>
  <div class="about">
    <div class="about-content">
      <h1 class="about-title">About <span class="gradient-text"><span class="logo-at">@</span>potheca</span></h1>
      <p class="about-lead">
        Apotheca is a personal productivity application designed to keep your notes, tasks,
        and ideas in one place — clean, fast, and always available.
      </p>

      <div class="about-sections">
        <div class="about-section">
          <h2><i class="pi pi-sparkles"></i> What it is</h2>
          <p>
            A focused note-taking and task management tool built for people who value clarity.
            No bloat, no distractions — just a well-organised space for your work and thoughts.
          </p>
        </div>

        <div class="about-section">
          <h2><i class="pi pi-wrench"></i> How it's built</h2>
          <p>
            Apotheca is a full-stack web application. The frontend is built with Vue 3 and PrimeVue.
            The backend is a .NET 10 API hosted on Google Cloud Run, with PostgreSQL for persistence.
          </p>
        </div>

        <div class="about-section">
          <h2><i class="pi pi-shield"></i> Privacy</h2>
          <p>
            Your data is private. Authentication is handled via Auth0, and all data is scoped
            to your account. Nothing is shared or sold.
          </p>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.about {
  flex: 1;
  display: flex;
  justify-content: center;
  padding: 4rem 2rem;
  overflow-y: auto;
}

.about-content {
  width: 100%;
  max-width: 700px;
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.about-title {
  font-size: 2.5rem;
  font-weight: 800;
  margin: 0;
  color: var(--text-primary);
}

.gradient-text {
  background: var(--gradient-brand);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.logo-at {
  font-style: italic;
}

.about-lead {
  font-size: 1.05rem;
  color: var(--text-secondary);
  line-height: 1.75;
  margin: 0;
}

.about-sections {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.about-section {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 14px;
  padding: 1.5rem 1.75rem;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.about-section:hover {
  border-color: var(--border-purple);
  box-shadow: 0 0 20px var(--glow-purple);
}

.about-section h2 {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  font-size: 1rem;
  font-weight: 600;
  margin: 0 0 0.75rem;
  background: var(--gradient-brand);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.about-section h2 i {
  background: var(--gradient-brand);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.about-section p {
  margin: 0;
  font-size: 0.9rem;
  color: var(--text-secondary);
  line-height: 1.7;
}
</style>
