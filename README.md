SocialMedia

This is a full-stack, real-time social media platform designed to connect users through an intuitive, modern interface. Built with a robust **ASP.NET Core** backend and a highly responsive **React** frontend, the platform features instant messaging, interactive feeds, dynamic notification systems, and secure user relationships.

---

## 🚀 Key Features

* **Secure Authentication:** Custom JWT-based authentication coupled with BCrypt password hashing.
* **Real-Time Interactions:** Powered by **SignalR** for instantaneous instant messaging, live typing indicators, and read receipts.
* **Robust Social Graph:** Comprehensive follow system featuring explicit pending, accepted, and rejected request workflows.
* **Dynamic Feed & Engagement:** Personalized post feeds with support for text content, likes, nesting comments, and media.
* **Media Management:** Seamless image uploading and processing integrated directly with **Cloudinary**.
* **Instant Notifications:** Real-time push alerts for likes, comments, and incoming connection requests.

---

## 🛠️ Tech Stack

### Backend
* **Framework:** ASP.NET Core (Web API)
* **Real-Time Communication:** SignalR
* **Database ORM:** Entity Framework Core (EF Core)
* **Database:** Supabase PostgreSQL
* **Logging:** Serilog
* **Containerization:** Docker

### Frontend
* **Library/Language:** React, TypeScript
* **Data Fetching & State Management:** TanStack Query (React Query)
* **Styling:** Tailwind CSS

---

## 🌐 Deployment & Infrastructure

* **Frontend Deployment:** Hosted on **Vercel** to provide an optimized, global CDN-driven delivery of static UI assets.
* **Backend Deployment:** Containerized with **Docker** and hosted on **Render**, ensuring uniform runtime environments and predictable deployments.
* **Data Tier:** Hosted on **Supabase (PostgreSQL)**, utilizing structured relational indexing to handle complex social operations efficiently.
