/** @jsxImportSource @emotion/react */
import { css } from '@emotion/react';
import { StyledBrandLink } from '../../styles';
import { DeepNavy } from '../../colors';
import { APPLICATION_NAME, SYSTEM_NAME } from '../constants/app-constants';

const TopNavBar: React.FC = () => (
  <header
    css={css`
      display: flex;
      justify-content: space-between;
      align-items: center;
      background-color: transparent;
      padding: 10px 20px;
      border-bottom: 1px solid ${DeepNavy};
    `}
  >
    <div>
      <StyledBrandLink to="/">
        {SYSTEM_NAME}
        <span>{APPLICATION_NAME}</span>
      </StyledBrandLink>
    </div>
  </header>
);

export default TopNavBar;
