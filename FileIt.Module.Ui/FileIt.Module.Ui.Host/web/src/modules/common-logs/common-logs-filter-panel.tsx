/** @jsxImportSource @emotion/react */
import { css } from '@emotion/react';
import { useMemo } from 'react';
import { getAllApplicationMappings } from '../../core/constants/application-mappings';

export interface CommonLogsFilterPanelProps {
  onApplicationChange: (application: string | null) => void;
  onTimePeriodChange: (timePeriod: string) => void;
  selectedApplication: string | null;
  selectedTimePeriod: string;
}

const filterPanelStyle = css`
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  margin-bottom: 1.5rem;
  padding: 1rem;
  background-color: #f5f5f5;
  border-radius: 4px;
`;

const filterRowStyle = css`
  display: flex;
  gap: 2rem;
  align-items: flex-start;
`;

const filterGroupStyle = css`
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
`;

const labelStyle = css`
  font-weight: 600;
  font-size: 14px;
  color: #333;
`;

const buttonGroupStyle = css`
  display: flex;
  gap: 0.5rem;
`;

const buttonStyle = (isActive: boolean) => css`
  padding: 0.5rem 1rem;
  border: 1px solid #ccc;
  background-color: ${isActive ? '#0078d4' : '#ffffff'};
  color: ${isActive ? '#ffffff' : '#333'};
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  font-weight: ${isActive ? '600' : '400'};
  transition: all 0.2s ease;

  &:hover {
    background-color: ${isActive ? '#0078d4' : '#e8e8e8'};
  }
`;

export const CommonLogsFilterPanel = ({
  onApplicationChange,
  onTimePeriodChange,
  selectedApplication,
  selectedTimePeriod,
}: CommonLogsFilterPanelProps) => {
  const applicationOptions = useMemo(() => getAllApplicationMappings(), []);

  const timeRanges = [
    { label: '1 Hour', value: '1h' },
    { label: '24 Hours', value: '24h' },
    { label: '7 Days', value: '7d' },
    { label: '30 Days', value: '30d' },
  ];

  return (
    <div css={filterPanelStyle}>
      <div css={filterRowStyle}>
        <div css={filterGroupStyle}>
          <label css={labelStyle}>Application</label>
          <div css={buttonGroupStyle}>
            {applicationOptions.map((app) => (
              <button
                key={app.long}
                css={buttonStyle((selectedApplication || '') === app.long)}
                onClick={() => onApplicationChange(app.long || null)}
              >
                {app.friendly}
              </button>
            ))}
          </div>
        </div>

        <div css={filterGroupStyle}>
          <label css={labelStyle}>Time Range</label>
          <div css={buttonGroupStyle}>
            {timeRanges.map((period) => (
              <button
                key={period.value}
                css={buttonStyle(selectedTimePeriod === period.value)}
                onClick={() => onTimePeriodChange(period.value)}
              >
                {period.label}
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
