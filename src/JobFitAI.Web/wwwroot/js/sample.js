// Example data for the "Try an example" button and the Step 2 preview.
// The job advert and CV are fictional.

window.JobFitSample = {
  jobAd: `Junior Software Developer — Karoo Digital (Pretoria, hybrid)

About the role
Karoo Digital builds web platforms for logistics and retail clients. We are looking for a junior developer to join our .NET team and grow with us.

What you'll do
- Build and maintain features in our ASP.NET Core web applications
- Write SQL queries and work with SQL Server databases
- Write unit tests and fix bugs reported by our QA team
- Take part in code reviews and daily stand-ups
- Help document features for clients

Requirements
- A qualification in Software Development, Computer Science or similar
- Working knowledge of C# and .NET
- Experience with SQL and relational databases
- Understanding of Git and version control
- Good written and verbal communication

Nice to have
- Experience with Azure or another cloud platform
- Exposure to React or another front-end framework
- Experience working in an Agile team`,

  cv: `Thandi Mokoena
Pretoria · thandi.mokoena@example.com

Profile
Final-year Software Development student who enjoys building practical web applications and working in small teams.

Skills
C#, .NET, ASP.NET Core, SQL Server, HTML, CSS, JavaScript, Git, unit testing (xUnit)

Projects
Clinic Booking System (team of 3) — ASP.NET Core web app for booking appointments. Designed the SQL Server database and built the booking API. Wrote unit tests for the booking rules.
Personal Budget Tracker — C# Windows Forms app that stores expenses in SQLite and shows monthly summaries.

Experience
IT Assistant (part-time), Hillcrest Pharmacy, 2025 — Set up workstations, supported staff with software issues and kept the stock spreadsheet up to date.

Education
Diploma in Software Development, 2024–2026 (final year)`,

  // The shape of this object is the contract the API will return in Step 3.
  result: {
    score: 74,
    verdict: "apply",            // "apply" | "stretch" | "skip"
    headline: "A strong match for the core .NET and SQL work. Close the cloud and Agile gaps in your application.",
    matches: [
      { requirement: "Working knowledge of C# and .NET", evidence: "Lists C#, .NET and ASP.NET Core, and built the Clinic Booking System in ASP.NET Core." },
      { requirement: "Experience with SQL and relational databases", evidence: "Designed the SQL Server database for the Clinic Booking System; used SQLite in the Budget Tracker." },
      { requirement: "Write unit tests and fix bugs", evidence: "Wrote unit tests for the booking rules using xUnit." },
      { requirement: "Understanding of Git and version control", evidence: "Git is listed under skills, and the team project implies shared version control." },
      { requirement: "A qualification in Software Development", evidence: "Final year of a Diploma in Software Development." }
    ],
    gaps: [
      { requirement: "Experience with Azure or another cloud platform", evidence: "Deploy one project to a free Azure or Render tier and mention it in your profile." },
      { requirement: "Experience working in an Agile team", evidence: "If the team project used stand-ups, sprints or a task board, say so explicitly." },
      { requirement: "Exposure to React or another front-end framework", evidence: "This is a nice-to-have. A small React page in a portfolio project would be enough." }
    ],
    summary: "Final-year Software Development student who builds ASP.NET Core and SQL Server applications. Designed the database and booking API for a three-person Clinic Booking System, covered the business rules with unit tests, and works daily with C#, .NET and Git. Looking to grow in a .NET team that values clean, tested code.",
    tips: [
      "Lead with the Clinic Booking System. It matches the job almost line for line.",
      "Name the testing framework and the number of tests you wrote, so the testing claim is concrete.",
      "Add a one-line link to the project's GitHub repo so the reviewer can see the code."
    ]
  }
};
