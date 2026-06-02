/** @jsxImportSource @emotion/react */
import { css } from '@emotion/react';
import { StyledNavLink, StyledBrandLink } from '../../styles';
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
        FileIt<span>{import.meta.env.VITE_REACT_APP_ENV || 'dev'}</span>
      </StyledBrandLink>
      <StyledNavLink to="/logs">Logs</StyledNavLink>
    </div>
  </header>
);

export default TopNavBar;
