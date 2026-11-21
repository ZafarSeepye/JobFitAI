// JobFit AI — front-end logic.
// Step 2: the page, input handling and the animated results report.
// The "Analyse fit" button shows preview data until the API is connected.

(() => {
  "use strict";

  const MAX_CHARS = 8000;
  const MIN_CHARS = 200;
  const VERDICT_LABELS = { apply: "APPLY", stretch: "STRETCH", skip: "SKIP" };

  const $ = (id) => document.getElementById(id);
  const els = {
    jobAd: $("jobAd"), cv: $("cv"),
    analyse: $("analyse"), example: $("example"), clear: $("clear"),
    formError: $("formError"), results: $("results"),
    scoreNum: $("scoreNum"), headline: $("headline"), previewNote: $("previewNote"),
    stamp: $("stamp"), stampText: $("stampText"),
    matches: $("matches"), gaps: $("gaps"),
    summary: $("summary"), copy: $("copy"), tips: $("tips")
  };

  const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const hasGsap = typeof window.gsap !== "undefined" && !reduceMotion;

  // ---------- character counters ----------
  function updateCount(textarea) {
    const counter = document.querySelector(`.count[data-for="${textarea.id}"]`);
    const n = textarea.value.length;
    counter.textContent = `${n.toLocaleString("en-ZA")} / ${MAX_CHARS.toLocaleString("en-ZA")}`;
    counter.classList.toggle("near", n > MAX_CHARS * 0.9);
  }
  [els.jobAd, els.cv].forEach((t) => {
    t.addEventListener("input", () => { updateCount(t); hideError(); });
    updateCount(t);
  });

  // ---------- validation ----------
  function showError(message) { els.formError.textContent = message; els.formError.hidden = false; }
  function hideError() { els.formError.hidden = true; }

  function validate() {
    const job = els.jobAd.value.trim();
    const cv = els.cv.value.trim();
    if (!job && !cv) return "Paste a job advert and a CV to analyse.";
    if (!job) return "Paste the job advert first.";
    if (!cv) return "Paste the CV first.";
    if (job.length < MIN_CHARS) return `The job advert is too short to analyse. Paste the full advert (at least ${MIN_CHARS} characters).`;
    if (cv.length < MIN_CHARS) return `The CV is too short to analyse. Paste the full CV (at least ${MIN_CHARS} characters).`;
    return null;
  }

  // ---------- button feedback ----------
  function press(button) {
    if (!hasGsap) return;
    gsap.fromTo(button, { scale: 0.96 }, { scale: 1, duration: 0.35, ease: "back.out(3)" });
  }
  function setLoading(isLoading) {
    els.analyse.disabled = isLoading;
    els.analyse.classList.toggle("is-loading", isLoading);
    els.analyse.querySelector(".btn-label").textContent = isLoading ? "Reading both documents" : "Analyse fit";
  }

  // ---------- rendering ----------
  function listItem(requirement, evidence) {
    const li = document.createElement("li");
    const req = document.createElement("span");
    req.className = "req";
    const mark = document.createElement("span");
    mark.className = "hl";
    mark.textContent = requirement;          // textContent, never innerHTML: user and AI text is untrusted
    req.appendChild(mark);
    const ev = document.createElement("span");
    ev.className = "evidence";
    ev.textContent = evidence;
    li.append(req, ev);
    return li;
  }

  function renderResult(result, { preview = false } = {}) {
    els.headline.textContent = result.headline;
    els.previewNote.hidden = !preview;

    els.matches.replaceChildren(...result.matches.map((m) => listItem(m.requirement, m.evidence)));
    els.gaps.replaceChildren(...result.gaps.map((g) => listItem(g.requirement, g.evidence)));
    els.summary.textContent = result.summary;
    els.tips.replaceChildren(...result.tips.map((t) => {
      const li = document.createElement("li"); li.textContent = t; return li;
    }));

    els.stamp.dataset.verdict = result.verdict;
    els.stampText.textContent = VERDICT_LABELS[result.verdict] ?? result.verdict.toUpperCase();

    els.results.hidden = false;
    animateIn(result.score);
  }

  // One orchestrated sequence: score counts up, the stamp lands,
  // then the highlighter strokes are drawn across each requirement.
  function animateIn(score) {
    const marks = els.results.querySelectorAll(".hl");

    if (!hasGsap) {
      els.scoreNum.textContent = score;
      marks.forEach((m) => m.classList.add("drawn"));
      return;
    }

    gsap.killTweensOf([els.stamp, marks]);
    const counter = { value: 0 };
    const tl = gsap.timeline();

    tl.from(".report", { y: 24, opacity: 0, duration: 0.45, ease: "power2.out" })
      .to(counter, {
        value: score, duration: 1.1, ease: "power2.out",
        onUpdate: () => { els.scoreNum.textContent = Math.round(counter.value); }
      }, "-=0.1")
      .fromTo(els.stamp,
        { scale: 2.6, opacity: 0, rotate: -22 },
        { scale: 1, opacity: 1, rotate: -8, duration: 0.42, ease: "power4.in" }, "-=0.35")
      .to(".report", { x: 3, duration: 0.05, yoyo: true, repeat: 3, ease: "none" })   // the thud
      .fromTo(marks,
        { backgroundSize: "0% 62%" },
        { backgroundSize: "100% 62%", duration: 0.5, ease: "power1.inOut", stagger: 0.12 }, "-=0.05");

    els.results.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  // ---------- actions ----------
  els.analyse.addEventListener("click", async () => {
    press(els.analyse);
    const error = validate();
    if (error) { showError(error); return; }
    hideError();
    setLoading(true);
    // Step 2: simulate the wait, then show preview data. Step 4 replaces this with a real API call.
    await new Promise((r) => setTimeout(r, 900));
    setLoading(false);
    renderResult(window.JobFitSample.result, { preview: true });
  });

  els.example.addEventListener("click", () => {
    press(els.example);
    els.jobAd.value = window.JobFitSample.jobAd;
    els.cv.value = window.JobFitSample.cv;
    [els.jobAd, els.cv].forEach(updateCount);
    hideError();
    if (hasGsap) gsap.from(".sheet", { y: 10, opacity: 0.4, duration: 0.4, stagger: 0.08, ease: "power2.out" });
  });

  els.clear.addEventListener("click", () => {
    press(els.clear);
    els.jobAd.value = ""; els.cv.value = "";
    [els.jobAd, els.cv].forEach(updateCount);
    els.results.hidden = true;
    hideError();
    els.jobAd.focus();
  });

  els.copy.addEventListener("click", async () => {
    press(els.copy);
    const label = els.copy.querySelector("span");
    try {
      await navigator.clipboard.writeText(els.summary.textContent);
      label.textContent = "Copied";
    } catch {
      // Clipboard can be blocked; fall back to selecting the text so the user can copy it.
      const range = document.createRange();
      range.selectNodeContents(els.summary);
      const sel = window.getSelection(); sel.removeAllRanges(); sel.addRange(range);
      label.textContent = "Selected";
    }
    setTimeout(() => { label.textContent = "Copy"; }, 1800);
  });
})();
