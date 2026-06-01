import { BrowserRouter, Routes, Route } from 'react-router-dom';
import TopNavBar from './core/layout/top-navbar';
import NotFoundPage from './not-found-page';
import CommonLogsPage from './modules/common-logs/common-logs-page';
import '@progress/kendo-theme-material/dist/all.css';

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <div>
        <TopNavBar />
        <Routes>
          <Route path="" element={<CommonLogsPage />} />
          <Route path="/logs" element={<CommonLogsPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </div>
    </BrowserRouter>
  );
};

export default App;
