import { useState } from 'react';
import { Grid, GridColumn } from '@progress/kendo-react-grid';
import type { GridDataStateChangeEvent, GridRowClickEvent } from '@progress/kendo-react-grid';
import { process } from '@progress/kendo-data-query';
import type { State } from '@progress/kendo-data-query';
import type { CommonLog } from '../../core/models/common-log.model';

export interface CommonLogGridProps {
  data: CommonLog[];
  onRowClick: (log: CommonLog) => void;
}

export const CommonLogGrid = ({ data, onRowClick }: CommonLogGridProps) => {
  const [dataState, setDataState] = useState<State>({ skip: 0, take: 10 });
  const [result, setResult] = useState(process(data, dataState));

  const handleDataStateChange = (event: GridDataStateChangeEvent) => {
    setDataState(event.dataState);
    setResult(process(data, event.dataState));
  };

  const handleRowClick = (event: GridRowClickEvent) => {
    onRowClick(event.dataItem);
  };

  return (
    <Grid
      size="small"
      style={{ height: 600 }}
      data={result}
      filterable={true}
      pageable={true}
      onDataStateChange={handleDataStateChange}
      onRowClick={handleRowClick}
      total={result.total}
      {...dataState}
    >
      <GridColumn field="id" title="Id" width={100} />
      <GridColumn field="application" title="Application" />
      <GridColumn field="invocationId" title="Invocation Id" />
      <GridColumn
        field="createdOn"
        title="Created"
        cells={{
          data: (props) => <td>{new Date(props.dataItem.createdOn).toLocaleString()}</td>,
        }}
        width={160}
      />
    </Grid>
  );
};
