import { BrowserRouter, Routes, Route } from 'react-router-dom';
import TopBar from './layout/TopBar';
import NotFoundPage from './NotFoundPage';
import DashboardPage from './features/Dashboard/DashboardPage';
import CommonLogsPage from './features/CommonLogs/CommonLogsPage';
import '@progress/kendo-theme-material/dist/all.css';

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <div>
        <TopBar />
        <Routes>
          <Route path="" element={<CommonLogsPage />} />
          <Route path="/logs" element={<CommonLogsPage />} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </div>
    </BrowserRouter>
  );
};

export default App;
