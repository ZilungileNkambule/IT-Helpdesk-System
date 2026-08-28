# IT Helpdesk System

A ticketing system for the Free State Department of Social Development, allowing staff to log IT issues and technicians/admins to manage, track, and resolve them. Built as a Flutter mobile app and an ASP.NET Core web app, sharing a single Supabase backend.

## Overview

Departmental staff currently report IT issues informally (phone, in-person, WhatsApp), with no way to track status or measure resolution time. This system replaces that with a structured ticketing workflow: staff submit tickets from the mobile app, technicians manage and resolve them from the web dashboard, and both apps stay in sync in real time through Supabase.

## Tech Stack

| Layer | Technology |
|---|---|
| Mobile app | Flutter |
| Web app | ASP.NET Core |
| Database & Auth | Supabase (PostgreSQL, Supabase Auth) |
| Realtime updates | Supabase Realtime |
| ORM | Entity Framework Core |

## Features

- Role-based accounts: **Staff**, **Technician**, **Admin**
- Staff can submit tickets with title, description, category, urgency, and an optional photo
- Technicians can view, assign, and update ticket status (Open → In Progress → Resolved → Closed)
- Full status history logged per ticket
- Real-time status updates pushed to staff as their ticket progresses
- In-app (and optional email) notifications on status changes
- Admin dashboard with ticket counts by category/status and average resolution time
- Exportable reports (CSV/PDF) for a selected date range

## Project Structure
├── docs/                # Project documentation (SRS, diagrams, data dictionary, etc.)
└── README.md
Getting Started
Prerequisites
Flutter SDK installed
.NET SDK installed
A Supabase project (free tier is sufficient)
Android device or emulator for mobile testing
Setup
Clone the repository
   git clone <repository-url>
   cd it-helpdesk-system
Configure Supabase
Create a project at supabase.com
Run the database schema (see docs/database-schema.sql) in the Supabase SQL editor
Copy your Supabase project URL and anon/public API key
Set up the web app
   cd web
   dotnet restore

Add your Supabase URL and key to appsettings.json, then run:

   dotnet run
Set up the mobile app
   cd mobile
   flutter pub get

Add your Supabase URL and key to the app's config file, then run:

   flutter run
User Roles
Role	Access
Staff	Submit tickets, view own ticket status and history
Technician	View assigned tickets, update status, add resolution notes
Admin	Full access — all tickets, reports, user/role management
Documentation

Full project documentation is in the docs/ folder, including:

Software Requirements Specification (SRS)
Use case, class, and ER diagrams
Data dictionary
User manual
Test plan
Status

This project is under active development as part of an academic software development module.
