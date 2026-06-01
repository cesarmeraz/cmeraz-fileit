import { useEffect, useState } from 'react';
import PageTitle from '../../shared/page-title';
import logData from '../../data/log-query-highlevel-output.json';
import { CommonLogsGrid } from './common-logs-grid';
import type { CommonLogServer } from '../../core/models/common-log.model';
import Page from '../../shared/page';

const CommonLogsPage: React.FC = () => {
  const [commonLogs, setCommonLogs] = useState<CommonLogServer[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    let cancelled = false;
    const doGetCommonLogs = async () => {
      if (!cancelled) {
        setCommonLogs(logData);
        setLoading(false);
      }
    };
    doGetCommonLogs();
    return () => {
      cancelled = true;
    };
  }, [commonLogs]);

  return (
    <Page>
      <PageTitle>Logs</PageTitle>
      {loading ? (
        <div>Loading...</div>
      ) : (
        <CommonLogsGrid data={commonLogs}></CommonLogsGrid>
      )}
      <div></div>
    </Page>
  );
};

export default CommonLogsPage;
