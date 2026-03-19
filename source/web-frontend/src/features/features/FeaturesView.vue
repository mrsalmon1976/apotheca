<template>
  <div class="features-page">
    <div class="features-content">
      <h1 class="page-title">Keeping it <span class="gradient-text">simple</span></h1>
      <p class="page-lead">
        Apotheca is built around the tools people actually use, without the clutter.  We've added the 
        core features that small teams need, without the clutter and complexity that comes with most of 
        the bloated project management tools available.
      </p>

      <div class="feature-grid">
        <div class="feature-card">
          <div class="feature-icon"><i class="pi pi-file-edit"></i></div>
          <h3>Notes</h3>
          <p>Capture ideas, write documentation, and organise your thoughts using folders and tags. Rich text support keeps formatting clean and readable.</p>
        </div>
        <div class="feature-card">
          <div class="feature-icon"><i class="pi pi-check-square"></i></div>
          <h3>Tasks</h3>
          <p>Track work across multiple projects with priorities, due dates, and status filters. Stay focused with a Today view that surfaces what matters now.</p>
        </div>
        <div class="feature-card">
          <div class="feature-icon"><i class="pi pi-folder"></i></div>
          <h3>Projects</h3>
          <p>Group notes and tasks into projects. Set goals, track progress, and keep everything related to a piece of work together in one place.</p>
        </div>
        <div class="feature-card">
          <div class="feature-icon"><i class="pi pi-users"></i></div>
          <h3>Teams</h3>
          <p>Form teams to collaborate on projects. Share notes, assign tasks, and track work together without losing the personal workspace you rely on.</p>
        </div>
        <div class="feature-card">
          <div class="feature-icon"><i class="pi pi-file"></i></div>
          <h3>Documents</h3>
          <p>Create long-form documents alongside your notes. Structured, version-tracked, and always linked to the project they belong to.</p>
        </div>
        <div class="feature-card">
          <div class="feature-icon"><i class="pi pi-lock"></i></div>
          <h3>Private &amp; Secure</h3>
          <p>Your data is scoped to your account and never shared. Authentication via Auth0 keeps access controlled and your workspace yours alone.</p>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.features-page {
  flex: 1;
  display: flex;
  justify-content: center;
  padding: 4rem 2rem;
  overflow-y: auto;
}

.features-content {
  width: 100%;
  max-width: 900px;
  display: flex;
  flex-direction: column;
  gap: 2.5rem;
}

.page-title {
  font-size: 2.5rem;
  font-weight: 800;
  margin: 0;
  color: var(--text-primary);
  line-height: 1.2;
}

.gradient-text {
  background: var(--gradient-brand);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.page-lead {
  font-size: 1.05rem;
  color: var(--text-secondary);
  line-height: 1.75;
  margin: 0;
  max-width: 600px;
}

.feature-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 1.25rem;
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
