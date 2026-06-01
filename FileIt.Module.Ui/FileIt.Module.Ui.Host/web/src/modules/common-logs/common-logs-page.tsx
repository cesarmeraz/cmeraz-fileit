import { useEffect, useState } from 'react';
import PageTitle from '../../shared/page-title';
import { CommonLogGrid } from './common-log-grid';
import type { CommonLog } from '../../core/models/common-log.model';
import Page from '../../shared/page';
import { getCommonLogs } from './common-logs.service';

const CommonLogsPage: React.FC = () => {
  const [commonLogs, setCommonLogs] = useState<CommonLog[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

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

  return (
    <Page>
      <PageTitle>Logs</PageTitle>
      {loading ? <div>Loading...</div> : <CommonLogGrid data={commonLogs}></CommonLogGrid>}
      <div></div>
    </Page>
  );
};

export default CommonLogsPage;
