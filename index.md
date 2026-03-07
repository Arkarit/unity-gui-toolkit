---
layout: default
title: UI Toolkit
---

<div class="hero">
  <h1>Unity GUI Toolkit</h1>
  <p>
    A comprehensive, production-ready UI library for Unity 2022.3+,
    built on uGUI and TextMeshPro — with a clean component model,
    skinning, state machines, pooling, and more.
  </p>
  <div class="hero-buttons">
    <a class="btn-primary" href="setup">Get Started</a>
    <a class="btn-secondary" href="Code/html/index.html">API Reference</a>
  </div>
</div>

<div class="notice">
  ⚠️ <b>This toolkit is a work in progress. APIs may change between versions.</b>
</div>

<div class="features-section">
  <h2>Core Features</h2>
  <div class="features-grid">

    <div class="feature-card">
      <div class="card-icon">🧱</div>
      <h3>Clean Component Hierarchy</h3>
      <p>A layered base class model — <code>UiThing</code> → <code>UiPanel</code> → <code>UiView</code> — gives every element a consistent lifecycle, event system, and RectTransform foundation.</p>
    </div>

    <div class="feature-card">
      <div class="card-icon">🎬</div>
      <h3>Show / Hide Lifecycle</h3>
      <p><code>UiPanel</code> provides a fully-managed Show/Hide API with pluggable animations, visibility events, and automatic pooling or destruction on hide.</p>
    </div>

    <div class="feature-card">
      <div class="card-icon">🎨</div>
      <h3>Skin &amp; Style System</h3>
      <p>Define named skins in a <code>UiStyleConfig</code> ScriptableObject. Switch skins at runtime — optionally with a tween — and every styled component updates automatically.</p>
    </div>

    <div class="feature-card">
      <div class="card-icon">🔀</div>
      <h3>UI State Machine</h3>
      <p>Record per-state property snapshots on any GameObject and animate between them via <code>UiStateMachine</code> and <code>UiTransition</code>, with full editor preview support.</p>
    </div>

    <div class="feature-card">
      <div class="card-icon">♻️</div>
      <h3>Built-in Object Pooling</h3>
      <p><code>UiPool</code> manages reusable prefab instances. Views created through <code>UiMain</code> are pooled automatically — implement <code>IPoolable</code> to hook into lease and return.</p>
    </div>

    <div class="feature-card">
      <div class="card-icon">🌍</div>
      <h3>Localization</h3>
      <p>A lightweight localization layer with a provider interface, plural support, and per-component language-change callbacks keeps your UI text in sync with the active language.</p>
    </div>

    <div class="feature-card">
      <div class="card-icon">✨</div>
      <h3>UI Mesh Modifiers</h3>
      <p>A rich set of vertex-level mesh effects — gradients, distortion, skew, bend, FFD, UV modifiers, and more — composable directly on any uGUI graphic.</p>
    </div>

    <div class="feature-card">
      <div class="card-icon">🗂️</div>
      <h3>Navigation &amp; Dialogs</h3>
      <p><code>UiMain</code> manages a view navigation stack and provides ready-made dialogs: OK/Yes-No requesters, toast messages, settings dialog, and tab dialogs.</p>
    </div>

  </div>
</div>
