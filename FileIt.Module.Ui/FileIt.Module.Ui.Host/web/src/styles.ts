import styled from '@emotion/styled';
import { Link } from 'react-router-dom';

const BaseLink = styled(Link)`
  text-decoration: none;
  padding: 8px 16px;
  border-radius: 4px;
  transition:
    background-color 0.3s ease,
    color 0.3s ease;
  color: lightgray;

  &:hover,
  &:active {
    color: white;
  }
`;

export const StyledNavLink = styled(BaseLink)`
  font-size: 18px;
  font-weight: 500;
`;

export const StyledBrandLink = styled(BaseLink)`
  font-size: 24px;
  font-weight: bold;

  span {
    margin-left: 5px;
    font-size: 14px;
    font-weight: normal;
  }
`;
