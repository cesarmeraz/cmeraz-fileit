import { Window } from '@progress/kendo-react-dialogs';
import type { CommonLogDetail } from '../../core/models/common-log.model';

export interface CommonLogDetailsModalProps {
  modalShowing: boolean;
  logDetails: CommonLogDetail[] | null;
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

export const CommonLogDetailsModal = ({ modalShowing, logDetails, onClose }: CommonLogDetailsModalProps) => {
  if (!modalShowing || !logDetails) {
    return null;
  }

  return (
    <Window title="Log Details" onClose={onClose} width={1000} height={700}>
      <div style={detailsContainerStyle}>
        <div style={relatedLogsContainerStyle}>
          <div style={tableContainerStyle}>
            <table style={tableStyle}>
              <thead>
                <tr style={{ backgroundColor: '#f5f5f5' }}>
                  <th style={tableHeaderCellStyle}>Id</th>
                  <th style={tableHeaderCellStyle}>Message</th>
                  <th style={tableHeaderCellStyle}>Level</th>
                  <th style={tableHeaderCellStyle}>Exception</th>
                  <th style={tableHeaderCellStyle}>Environment</th>
                  <th style={tableHeaderCellStyle}>MachineName</th>
                  <th style={tableHeaderCellStyle}>Application</th>
                  <th style={tableHeaderCellStyle}>Application Version</th>
                  <th style={tableHeaderCellStyle}>Infrastructure Version</th>
                  <th style={tableHeaderCellStyle}>Source Context</th>
                  <th style={tableHeaderCellStyle}>Correlation Id</th>
                  <th style={tableHeaderCellStyle}>Invocation Id</th>
                  <th style={tableHeaderCellStyle}>Event Name</th>
                  <th style={tableHeaderCellStyle}>Created On</th>
                </tr>
              </thead>
              <tbody>
                {logDetails.map((log, index) => (
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
                    <td style={tableCellStyle}>{log.message}</td>
                    <td style={tableCellStyle}>{log.level}</td>
                    <td style={tableCellStyle}>{log.exception}</td>
                    <td style={tableCellStyle}>{log.environment}</td>
                    <td style={tableCellStyle}>{log.machineName}</td>
                    <td style={tableCellStyle}>{log.application}</td>
                    <td style={tableCellStyle}>{log.applicationVersion}</td>
                    <td style={tableCellStyle}>{log.infrastructureVersion}</td>
                    <td style={tableCellStyle}>{log.sourceContext}</td>
                    <td style={tableCellStyle}>{log.correlationId}</td>
                    <td style={tableCellStyle}>{log.invocationId}</td>
                    <td style={tableCellStyle}>{log.eventName}</td>
                    <td style={tableCellStyle}>{log.createdOn.toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {logDetails.length === 0 && (
              <div style={{ padding: '16px', textAlign: 'center', color: '#999' }}>No log details found</div>
            )}
          </div>
        </div>
      </div>
    </Window>
  );
};
