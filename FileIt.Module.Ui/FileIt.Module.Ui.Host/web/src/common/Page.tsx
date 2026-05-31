/** @jsxImportSource @emotion/react */
import { css } from '@emotion/react';
import PageTitle from './PageTitle';

interface PageProps {
  title?: string;
  children: React.ReactNode;
}

const containerStyle = css`
  margin: 1.5rem auto;
  padding: 0 1.5rem;
  width: 100%;
  max-width: 1200px;
`;

const Page: React.FC<PageProps> = ({ title, children }: PageProps) => (
  <div css={containerStyle}>
    {title && <PageTitle>{title}</PageTitle>}
    <div>{children}</div>
  </div>
);

export default Page;
