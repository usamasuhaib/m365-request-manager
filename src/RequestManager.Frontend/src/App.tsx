import React, { useEffect, useState } from "react";
import { HashRouter as Router, Routes, Route } from "react-router-dom";
import { FluentProvider, webLightTheme } from "@fluentui/react-components";
import { initializeTeams } from "./services/authService";
import Dashboard from "./pages/Dashboard";
import CreateRequest from "./pages/CreateRequest";
import RequestDetails from "./pages/RequestDetails";
import OutlookPane from "./pages/OutlookPane";

export default function App() {
  const [initializing, setInitializing] = useState(true);

  useEffect(() => {
    // Bootstrap Teams SDK context if running inside Teams tab frame
    initializeTeams().finally(() => {
      setInitializing(false);
    });
  }, []);

  if (initializing) {
    return (
      <div style={{ display: "flex", justifyContent: "center", alignItems: "center", height: "100vh" }}>
        <h3>Loading Request Manager...</h3>
      </div>
    );
  }

  return (
    <FluentProvider theme={webLightTheme}>
      <Router>
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/create" element={<CreateRequest />} />
          <Route path="/request/:id" element={<RequestDetails />} />
          <Route path="/outlook-pane" element={<OutlookPane />} />
        </Routes>
      </Router>
    </FluentProvider>
  );
}
