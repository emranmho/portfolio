import Link from "next/link";

type Variant = "solid" | "ghost";

const styles: Record<Variant, string> = {
  solid:
    "bg-accent text-bg hover:opacity-90 border border-transparent font-semibold",
  ghost:
    "border border-border text-text hover:border-accent hover:text-accent bg-transparent",
};

const base =
  "inline-flex items-center gap-2 rounded px-4 py-2 font-mono text-sm transition-colors";

export function Button({
  href,
  variant = "solid",
  children,
  ...rest
}: {
  href?: string;
  variant?: Variant;
  children: React.ReactNode;
} & React.ButtonHTMLAttributes<HTMLButtonElement>) {
  const className = `${base} ${styles[variant]}`;
  if (href) {
    // External links open in a new tab; internal ones client-navigate.
    if (/^https?:\/\//.test(href)) {
      return (
        <a
          href={href}
          target="_blank"
          rel="noopener noreferrer"
          className={className}
        >
          {children}
        </a>
      );
    }
    return (
      <Link href={href} className={className}>
        {children}
      </Link>
    );
  }
  return (
    <button className={className} {...rest}>
      {children}
    </button>
  );
}
