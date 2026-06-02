import { Dialog } from '@progress/kendo-react-dialogs';
import type { CommonLog } from '../../core/models/common-log.model';

export interface CommonLogDetailsModalProps {
  modalShowing: boolean;
  selectedLog: CommonLog | null;
  relatedLogs?: CommonLog[];
  onClose: () => void;
}

const detailsContainerStyle: React.CSSProperties = {
  padding: '16px',
  display: 'flex',
  flexDirection: 'column',
  height: '100%',
};

const relatedLogsContainerStyle: React.CSSProperties = {
  flex: 1,
  display: 'flex',
  flexDirection: 'column',
  overflow: 'hidden',
};

const tableHeaderStyle: React.CSSProperties = {
  fontWeight: 'bold',
  marginBottom: '8px',
  color: '#333',
  fontSize: '12px',
  textTransform: 'uppercase',
  letterSpacing: '0.5px',
};

const tableContainerStyle: React.CSSProperties = {
  flex: 1,
  overflow: 'auto',
  borderTop: '1px solid #e0e0e0',
  borderRadius: '4px',
};

const tableStyle: React.CSSProperties = {
  width: '100%',
  borderCollapse: 'collapse',
  fontSize: '13px',
};

const tableHeaderCellStyle: React.CSSProperties = {
  backgroundColor: '#f5f5f5',
  padding: '8px',
  textAlign: 'left',
  fontWeight: 'bold',
  borderBottom: '1px solid #ddd',
  color: '#333',
};

const tableRowStyle: React.CSSProperties = {
  borderBottom: '1px solid #e0e0e0',
};

const tableCellStyle: React.CSSProperties = {
  padding: '8px',
  color: '#666',
};

export const CommonLogDetailsModal = ({
  modalShowing,
  selectedLog,
  relatedLogs = [],
  onClose,
}: CommonLogDetailsModalProps) => {
  if (!modalShowing || !selectedLog) {
    return null;
  }

  return (
    <Dialog title="Log Details" onClose={onClose} width={1000} height={700}>
      <div style={detailsContainerStyle}>
        {/* Related Logs Section */}
        <div style={relatedLogsContainerStyle}>
          <div style={tableHeaderStyle}>Related Logs</div>
          <div style={tableContainerStyle}>
            <table style={tableStyle}>
              <thead>
                <tr style={{ backgroundColor: '#f5f5f5' }}>
                  <th style={tableHeaderCellStyle}>Id</th>
                  <th style={tableHeaderCellStyle}>Application</th>
                  <th style={tableHeaderCellStyle}>Invocation Id</th>
                  <th style={tableHeaderCellStyle}>Event Name</th>
                  <th style={tableHeaderCellStyle}>Created</th>
                </tr>
              </thead>
              <tbody>
                {relatedLogs.slice(0, 10).map((log, index) => (
                  <tr
                    key={`${log.id}-${index}`}
                    style={tableRowStyle}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.backgroundColor = '#fafafa';
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.backgroundColor = 'transparent';
                    }}
                  >
                    <td style={tableCellStyle}>{log.id}</td>
                    <td style={tableCellStyle}>{log.application}</td>
                    <td style={tableCellStyle}>{log.invocationId}</td>
                    <td style={tableCellStyle}>{log.eventName}</td>
                    <td style={tableCellStyle}>{new Date(log.createdOn).toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {relatedLogs.length === 0 && (
              <div style={{ padding: '16px', textAlign: 'center', color: '#999' }}>No related logs found</div>
            )}
          </div>
        </div>
      </div>
    </Dialog>
  );
};
