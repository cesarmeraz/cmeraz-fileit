import { BrowserRouter, Routes, Route } from 'react-router-dom';
import TopBar from './layout/top-bar';
import NotFoundPage from './not-found-page';
import CommonLogsPage from './modules/common-logs/common-logs-page';
import '@progress/kendo-theme-material/dist/all.css';

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <div>
        <TopBar />
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
