import { useEffect, useState } from 'react';
import Page from '../../shared/page';
import { CommonLogGrid } from './common-log-grid';
import { CommonLogDetailsModal } from './common-log-details-modal';
import type { CommonLog, CommonLogDetail } from '../../core/models/common-log.model';
import { getCommonLogs, getCommonLogDetails } from './common-logs.service';

const CommonLogsPage: React.FC = () => {
  const [commonLogs, setCommonLogs] = useState<CommonLog[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [, setSelectedLog] = useState<CommonLog | null>(null);
  const [selectedLogDetails, setSelectedLogDetails] = useState<CommonLogDetail[] | null>(null);
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

  const handleRowClick = async (log: CommonLog) => {
    setSelectedLog(log);
    const commonLogDetails = await getCommonLogDetails(log.invocationId);
    setSelectedLogDetails(commonLogDetails);
    setDialogOpen(true);
  };

  const handleDialogClose = () => {
    setDialogOpen(false);
    setSelectedLog(null);
  };

  return (
    <Page>
      {loading ? <div>Loading...</div> : <CommonLogGrid data={commonLogs} onRowClick={handleRowClick} />}
      <CommonLogDetailsModal modalShowing={dialogOpen} logDetails={selectedLogDetails} onClose={handleDialogClose} />
    </Page>
  );
};

export default CommonLogsPage;
