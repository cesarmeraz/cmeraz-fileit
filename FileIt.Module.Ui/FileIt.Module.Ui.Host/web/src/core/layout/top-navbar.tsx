/** @jsxImportSource @emotion/react */
import { css } from '@emotion/react';
import { StyledBrandLink } from '../../styles';
import { DeepNavy } from '../../colors';

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
        {import.meta.env.VITE_SYSTEM_NAME}
        <span>{import.meta.env.VITE_APP_NAME}</span>
      </StyledBrandLink>
    </div>
  </header>
);

export default TopNavBar;
