import Link from "next/link";

// 404 rendered as the ProblemDetails payload the API itself would return.
export default function NotFound() {
  return (
    <div className="py-16">
      <div className="max-w-xl overflow-x-auto border border-border bg-surface p-5 font-mono text-sm leading-relaxed">
        <div className="mb-3 text-err">HTTP/1.1 404 Not Found</div>
        <pre className="text-text-dim">{`{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "This page does not exist — but the API is still up."
}`}</pre>
      </div>
      <Link
        href="/"
        className="mt-6 inline-block font-mono text-sm text-accent hover:underline"
      >
        ← GET /
      </Link>
    </div>
  );
}
