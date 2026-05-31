interface PageTitleProps {
  children: React.ReactNode;
}

const PageTitle: React.FC<PageTitleProps> = ({ children }: PageTitleProps) => (
  <h2>{children}</h2>
);

export default PageTitle;
