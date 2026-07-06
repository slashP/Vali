import { useEffect, useState } from "react";
import { checkForUpdate, type UpdateInfo } from "../lib/update";
import { openExternal } from "../lib/commands";

const LS_DISMISSED = "vali.updateDismissed";

export function UpdateBanner() {
  const [info, setInfo] = useState<UpdateInfo | null>(null);

  useEffect(() => {
    let alive = true;
    void checkForUpdate().then((u) => {
      if (!alive || !u) return;
      // Respect a per-version dismissal so we don't nag about the same release.
      if (localStorage.getItem(LS_DISMISSED) === u.latest) return;
      setInfo(u);
    });
    return () => {
      alive = false;
    };
  }, []);

  if (!info) return null;

  const dismiss = () => {
    localStorage.setItem(LS_DISMISSED, info.latest);
    setInfo(null);
  };

  return (
    <div className="update-banner">
      <span className="update-dot" />
      <span className="update-text">
        Vali Studio <strong>v{info.latest}</strong> is available
        <span className="update-cur"> · you have v{info.current}</span>
      </span>
      <span className="grow" />
      <a
        className="update-link"
        href={info.url}
        onClick={(e) => {
          e.preventDefault();
          void openExternal(info.url);
        }}
      >
        Download
      </a>
      <button
        className="update-dismiss"
        onClick={dismiss}
        aria-label="Dismiss update notice"
        title="Dismiss"
      >
        ×
      </button>
    </div>
  );
}
