# sports-ranking
A comprehensive NFL depth chart management system built with dotnet8. This application allows teams to manage player positions and depth rankings with a modern web interface.

## 🏈 Overview

This project implements a depth chart management system where:
- Players are ranked by depth at each position (starter, backup, etc.)
- Positions are grouped by unit (Offense, Defense, Special Teams)
- Real-time updates via a responsive web interface

### Key Features

- Add/Remove Players - Manage player assignments to positions
- Depth Chart Visualization - Multi-column view showing player rankings
- Backup Player Lookup - Find all players behind a specific player

## 🚀 Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Any modern web browser

### Running the Application

1. **Clone the repository**
```
git clone https://github.com/lorna-jacob/sports-ranking.git cd sports-ranking
```

2. **Run the application**
```
dotnet run --project src/DepthCharts.Api/DepthCharts.Api.csproj
```

3. **Open your browser**
- Navigate to `https://localhost:7214` or `http://localhost:5155`

### Sample Data

The application comes pre-seeded with Tampa Bay Buccaneers data including:
- **Players**: Tom Brady, Mike Evans, Rob Gronkowski, and more
- **Positions**: NFL positions (QB, RB, LWR, TE, etc.)
- **Depth Chart**: Sample depth chart entries for demonstration
- 
## 📋 Assumptions & Design Decisions

- Currently, this app is designed for a single user (probably a team manager) who manages the depth chart. Data are stored in JSON files locally.
- However, this can be easily extended to have a database backend in the future by adding another implementation of `IDepthChartRepository`
- The API would be hosted in a cloud service like AWS for scalability and availability. The API can be consumed by multiple client applications in the future, ie mobile apps.
- Current implementation focuses on present state (no historical depth chart tracking)
- Zero-based depth indexing for adding player (0 = starter, 1 = first backup, and so on.), but non-zero-based indexing for removing player (1 = starter, 2 = first backup, and so on.) to align with user expectations.