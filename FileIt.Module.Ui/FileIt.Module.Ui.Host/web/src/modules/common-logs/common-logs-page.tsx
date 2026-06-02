import { useEffect, useState } from 'react';
import PageTitle from '../../shared/page-title';
import { CommonLogGrid } from './common-log-grid';
import { CommonLogDetailsModal } from './common-log-details-modal';
import type { CommonLog } from '../../core/models/common-log.model';
import Page from '../../shared/page';
import { getCommonLogs } from './common-logs.service';

const CommonLogsPage: React.FC = () => {
  const [commonLogs, setCommonLogs] = useState<CommonLog[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [selectedLog, setSelectedLog] = useState<CommonLog | null>(null);
  const [dialogOpen, setDialogOpen] = useState<boolean>(false);

  useEffect(() => {
    let cancelled = false;
    const doGetCommonLogs = async () => {
      if (!cancelled) {
        const commonLogs = await getCommonLogs();
        setCommonLogs(commonLogs);
        setLoading(false);
      }
    };
    doGetCommonLogs();
    return () => {
      cancelled = true;
    };
  }, []);

  const handleRowClick = (log: CommonLog) => {
    setSelectedLog(log);
    setDialogOpen(true);
  };

  const handleDialogClose = () => {
    setDialogOpen(false);
    setSelectedLog(null);
  };

  return (
    <Page>
      <PageTitle>Logs</PageTitle>
      {loading ? <div>Loading...</div> : <CommonLogGrid data={commonLogs} onRowClick={handleRowClick} />}
      <CommonLogDetailsModal
        modalShowing={dialogOpen}
        selectedLog={selectedLog}
        relatedLogs={
          selectedLog ? commonLogs.filter((log) => log.application === selectedLog.application).slice(0, 10) : []
        }
        onClose={handleDialogClose}
      />
    </Page>
  );
};

export default CommonLogsPage;
