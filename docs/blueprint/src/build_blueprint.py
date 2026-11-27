"""Builds the SmartFactory AI Solution Blueprint as HTML, ready for Chromium to print to PDF.

Usage:
    python3 build_blueprint.py --out ../build/blueprint.html [--fonts-css path/to/inter-local.css]
    node render.js ../build/blueprint.html ../smartfactory-ai-blueprint-v2.1.pdf

The document is data-driven: `content_a/b/c.py` hold one Python function per page that returns the
page's main HTML using the helpers below. Design tokens (colours, type scale, spacing) live in CSS()
and were measured from the v2.0 PDF so revisions keep the same look.

Everything in the document describes a portfolio concept on synthetic data. The system is advisory only.
"""

from __future__ import annotations

import argparse
import html
from pathlib import Path

VERSION = "2.1"
VERSION_LABEL = f"v{VERSION}"
ISSUE_DATE = "01 November 2026"
ISSUE_MONTH = "NOV 2026"
DOC_ID = "SMARTFACTORY-BP-002"
FOOTER_TEXT = "Portfolio concept / No autonomous machinery control"

# ---------------------------------------------------------------------------------------------
# Design tokens
# ---------------------------------------------------------------------------------------------
NAVY = "#0a2138"
INK = "#1d2a37"
MUTED = "#5c6a78"
TEAL = "#1a9c86"
TEAL_DARK = "#15826f"
TEAL_SOFT = "#e9f6f2"
TEAL_LINE = "#8fcdbf"
ORANGE = "#f0a24a"
RED = "#df6a5b"
ZEBRA = "#f6f8fa"
PANEL = "#f2f5f8"
TRACK = "#e5eaee"
RULE = "#d9e0e6"
CODE_BG = "#0f2a40"
CODE_FG = "#dae7f0"


def CSS(fonts_css: str) -> str:
    return f"""
{fonts_css}
*{{box-sizing:border-box}}
html,body{{margin:0;padding:0;background:#fff;color:{INK};font-family:'Inter',system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;
  font-size:12px;line-height:1.42;-webkit-print-color-adjust:exact;print-color-adjust:exact}}
@page{{size:A4;margin:0}}
.page{{position:relative;width:210mm;height:297mm;padding:19mm 17mm 16mm 17mm;page-break-after:always;overflow:hidden;background:#fff}}
.page:last-child{{page-break-after:auto}}
.topbar{{position:absolute;left:0;top:0;width:100%;height:2.2mm;background:{NAVY}}}
.hdr{{position:absolute;left:17mm;right:17mm;top:6.5mm;height:7mm;display:flex;justify-content:space-between;align-items:center;
  border-bottom:1px solid {RULE};font-size:10.6px;font-weight:700;letter-spacing:.09em;color:{NAVY}}}
.hdr .sep{{color:{MUTED};font-weight:500;margin:0 .45em}}
.hdr .bar{{color:{MUTED};font-weight:400;margin:0 .5em}}
.hdr .ver{{color:{MUTED};font-weight:600}}
footer{{position:absolute;left:17mm;right:17mm;bottom:7mm;display:flex;justify-content:space-between;border-top:1px solid {RULE};
  padding-top:2.6mm;font-size:10.8px;color:{MUTED}}}
main{{position:relative}}
.kicker{{font-size:10.6px;font-weight:700;letter-spacing:.1em;color:{TEAL};text-transform:uppercase;margin:0 0 3px}}
h1{{font-size:27px;line-height:1.12;font-weight:700;color:{NAVY};margin:0 0 4px;letter-spacing:-.01em}}
.sub{{font-size:13.2px;color:{MUTED};margin:0 0 10px}}
.rule{{border:0;border-top:1px solid {RULE};margin:0 0 12px}}
h3{{font-size:16.5px;font-weight:700;color:{NAVY};margin:14px 0 7px}}
p{{margin:0 0 8px;font-size:12.4px}}
p.lead{{font-size:12.6px}}
p.note{{font-size:11.2px;color:{MUTED};font-style:italic;margin:6px 0 10px}}
p.small{{font-size:11.4px;margin:6px 0 8px}}
strong{{font-weight:700;color:{NAVY}}}
code{{font-family:'DejaVu Sans Mono',Menlo,Consolas,monospace;font-size:10.4px;background:#eef2f5;padding:1px 4px;border-radius:3px;color:{INK}}}
table.t{{width:100%;border-collapse:collapse;margin:0 0 10px;font-size:11.4px}}
table.t th{{background:{NAVY};color:#fff;font-weight:600;text-align:left;padding:7px 9px;font-size:11px;vertical-align:bottom}}
table.t td{{padding:6.5px 9px;vertical-align:top;border-bottom:1px solid {RULE}}}
table.t tr:nth-child(even) td{{background:{ZEBRA}}}
table.t td:first-child{{font-weight:600;color:{NAVY}}}
table.t.plain td:first-child{{font-weight:400;color:{INK}}}
table.t td.b{{font-weight:600;color:{NAVY}}}
table.t.compact td{{padding:5px 8px}}
table.t.compact th{{padding:6px 8px}}
table.t.xs{{font-size:10.6px}}
table.t.xs td{{padding:4px 8px}}
table.t.xs th{{padding:5px 8px;font-size:10.4px}}
table.t.xxs{{font-size:10px}}
table.t.xxs td{{padding:2.8px 7px}}
table.t.xxs th{{padding:4.5px 7px;font-size:10px}}
.callout{{background:{TEAL_SOFT};border-left:3px solid {TEAL};padding:9px 12px 9px 13px;margin:10px 0 12px;font-size:12.2px}}
.callout .ct{{font-weight:700;color:{NAVY};margin-bottom:3px;font-size:12.4px}}
ul.d{{list-style:none;margin:0 0 8px;padding:0}}
ul.d li{{position:relative;padding-left:15px;margin:0 0 5px;font-size:12.2px}}
ul.d li::before{{content:'';position:absolute;left:1px;top:6px;width:7px;height:7px;border-radius:50%;background:{TEAL}}}
ul.d.tight li{{margin-bottom:3px}}
.cols2{{display:grid;grid-template-columns:1fr 1fr;gap:0 22px}}
.flow{{display:flex;align-items:stretch;gap:0;margin:8px 0 6px}}
.flow .fc{{flex:1;border:1px solid {RULE};border-radius:7px;padding:9px 8px;text-align:center;background:#fff;box-shadow:0 1px 2px rgba(10,33,56,.06)}}
.flow .fc.hi{{background:{TEAL_SOFT};border-color:{TEAL_LINE}}}
.flow .fc .ft{{font-weight:700;color:{NAVY};font-size:12.2px;margin-bottom:2px}}
.flow .fc .ft.caps{{text-transform:uppercase;letter-spacing:.05em;font-size:11.4px}}
.flow .fc .fs{{font-size:10.6px;color:{MUTED}}}
.flow .arr{{display:flex;align-items:center;justify-content:center;width:16px;color:{TEAL};font-size:16px;font-weight:700}}
.panel{{background:{PANEL};border-radius:9px;padding:12px 14px;margin:8px 0 12px}}
.panel .pt{{font-weight:700;color:{NAVY};font-size:12.6px;margin-bottom:8px}}
.kpis{{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin:6px 0 12px}}
.kpi{{background:{PANEL};border-radius:9px;padding:10px}}
.kpi div{{background:#fff;border:1px solid {RULE};border-radius:7px;text-align:center;padding:7px 6px;margin-bottom:8px}}
.kpi div:last-child{{margin-bottom:0}}
.kpi .kl{{font-weight:700;color:{NAVY};font-size:12.4px}}
.kpi .kv{{font-weight:700;color:{TEAL_DARK};font-size:17px}}
.kpi .kn{{font-size:10.4px;color:{MUTED};font-style:italic}}
.bars{{display:grid;grid-template-columns:150px 1fr 48px;row-gap:7px;column-gap:12px;align-items:center;font-size:11.6px}}
.bars .bl{{font-weight:600;color:{NAVY}}}
.bars .bt{{height:13px;border-radius:7px;background:{TRACK};overflow:hidden;display:flex}}
.bars .bf{{height:100%;border-radius:7px}}
.bars .bf.stack{{border-radius:0}}
.bars .bf.stack:first-child{{border-radius:7px 0 0 7px}}
.bars .bf.stack:last-child{{border-radius:0 7px 7px 0}}
.bars .bv{{text-align:right;color:{MUTED};font-size:11px}}
pre.code{{background:{CODE_BG};color:{CODE_FG};font-family:'DejaVu Sans Mono',Menlo,Consolas,monospace;font-size:10.2px;line-height:1.5;
  border-radius:8px;padding:12px 14px;margin:8px 0 12px;white-space:pre-wrap;word-break:break-word}}
.gantt{{display:grid;grid-template-columns:160px repeat(12,1fr);gap:4px 3px;align-items:center;margin:4px 0 14px;font-size:11.4px}}
.gantt .gh{{text-align:center;color:{MUTED};font-size:10.6px;padding-bottom:2px}}
.gantt .gl{{font-weight:600;color:{NAVY}}}
.gantt .gt{{grid-column:2 / span 12;height:14px;background:{TRACK};border-radius:7px;position:relative}}
.gantt .gb{{position:absolute;top:0;height:100%;border-radius:7px}}
.plant{{background:{PANEL};border-radius:9px;padding:12px 14px;margin:6px 0 10px}}
.plant .root{{width:235px;margin:0 auto 10px;background:{TEAL_SOFT};border:1px solid {TEAL_LINE};border-radius:7px;text-align:center;padding:8px}}
.plant .root .ft,.plant .line .ft{{font-weight:700;color:{NAVY};font-size:11.4px;letter-spacing:.05em;text-transform:uppercase}}
.plant .root .fs,.plant .line .fs{{font-size:10.4px;color:{MUTED}}}
.plant .lines{{display:grid;grid-template-columns:1fr 1fr;gap:18px}}
.plant .line{{border:1px solid {RULE};border-radius:7px;background:#fff;text-align:center;padding:7px;margin-bottom:8px}}
.plant table.t{{margin:0;font-size:11px}}
.cover{{background:radial-gradient(ellipse at 70% 35%,#123a5a 0%,#0c2a44 40%,#081d31 100%);color:#fff;padding:0}}
.cover .grid{{position:absolute;inset:0;background-image:linear-gradient(rgba(159,212,200,.07) 1px,transparent 1px),linear-gradient(90deg,rgba(159,212,200,.07) 1px,transparent 1px);
  background-size:38px 38px;transform:skewY(-6deg) scale(1.3);transform-origin:center}}
.cover .ck{{position:absolute;left:17mm;top:24mm;font-size:11.4px;font-weight:600;letter-spacing:.14em;color:#9ed4c8;text-transform:uppercase}}
.cover .ck .s{{color:#5c8f88;margin:0 .6em}}
.cover .ck::after{{content:'';display:block;width:150px;height:2px;background:{TEAL};margin-top:9px}}
.cover .art{{position:absolute;left:0;right:0;top:56mm;height:110mm}}
.cover .ttl{{position:absolute;left:17mm;right:17mm;top:196mm;border-top:1px solid rgba(159,212,200,.25);padding-top:14mm}}
.cover h1.big{{font-size:50px;color:#fff;margin:0 0 10px;letter-spacing:-.02em}}
.cover .st{{font-size:22px;font-weight:600;color:#e6f1f0;line-height:1.25;margin:0 0 20px}}
.cover .tags{{font-size:12px;color:#c8dae3;margin-bottom:10px}}
.cover .tags .s{{color:#4f6f80;margin:0 .6em}}
.cover .meta{{font-size:11.6px;color:#9eb3bf;margin-bottom:7px}}
.cover .meta2{{font-size:10.8px;color:#8aa0ae}}
.dash{{background:{NAVY};border-radius:10px;padding:10px 12px;color:#dae7f0;font-size:9.6px;margin:6px 0 4px}}
.dash .top{{display:flex;justify-content:space-between;align-items:center;margin-bottom:8px}}
.dash .top b{{color:#fff;font-size:11px;margin-right:6px}}
.dash .body{{display:grid;grid-template-columns:118px 1fr;gap:8px}}
.dash .nav div{{padding:4px 7px;border-radius:4px;color:#c8dae3;margin-bottom:2px}}
.dash .nav div.on{{background:{TEAL};color:#fff;font-weight:600}}
.dash .tiles{{display:grid;grid-template-columns:repeat(4,1fr);gap:7px;margin-bottom:8px}}
.dash .tile{{background:#12324c;border-radius:6px;padding:7px 9px}}
.dash .tile .l{{font-size:8.4px;letter-spacing:.06em;color:#9eb3bf;text-transform:uppercase}}
.dash .tile .v{{font-size:17px;font-weight:700;color:#fff}}
.dash .tile .v.o{{color:{ORANGE}}} .dash .tile .v.g{{color:#4fd1b8}}
.dash .row2{{display:grid;grid-template-columns:3fr 2fr;gap:7px;margin-bottom:8px}}
.dash .card{{background:#12324c;border-radius:6px;padding:8px 9px}}
.dash .card .l{{font-size:9.4px;color:#dae7f0;margin-bottom:4px}}
.dash .risk{{display:grid;grid-template-columns:52px 1fr 34px;gap:4px 6px;align-items:center;font-size:9.2px}}
.dash .risk .tr{{height:8px;background:#1d4360;border-radius:4px;overflow:hidden}}
.dash .risk .fi{{height:100%;border-radius:4px}}
.dash table{{width:100%;border-collapse:collapse;font-size:9px}}
.dash th{{text-align:left;color:#9eb3bf;font-weight:500;font-size:8.2px;letter-spacing:.05em;padding:3px 4px;border-bottom:1px solid #1d4360}}
.dash td{{padding:4px 4px;border-bottom:1px solid #16384f;color:#dae7f0;vertical-align:top}}
.dash td.warn{{color:{ORANGE}}}
.otflag{{color:{RED};font-weight:700;letter-spacing:.06em;font-size:11px;text-transform:uppercase;margin-right:8px}}
.tagline{{font-size:11.6px;color:{INK};margin:8px 0 0}}
"""


# ---------------------------------------------------------------------------------------------
# Helpers used by the content modules
# ---------------------------------------------------------------------------------------------
def esc(s: str) -> str:
    return html.escape(s, quote=False)


def table(headers: list[str], rows: list[list[str]], widths: list[str] | None = None, cls: str = "") -> str:
    colgroup = ""
    if widths:
        colgroup = "<colgroup>" + "".join(f'<col style="width:{w}">' for w in widths) + "</colgroup>"
    head = "".join(f"<th>{h}</th>" for h in headers)
    body = "".join("<tr>" + "".join(f"<td>{c}</td>" for c in r) + "</tr>" for r in rows)
    return f'<table class="t {cls}">{colgroup}<thead><tr>{head}</tr></thead><tbody>{body}</tbody></table>'


def callout(title: str, body: str) -> str:
    return f'<div class="callout"><div class="ct">{title}</div><div>{body}</div></div>'


def bullets(items: list[str], tight: bool = False) -> str:
    cls = "d tight" if tight else "d"
    return f'<ul class="{cls}">' + "".join(f"<li>{i}</li>" for i in items) + "</ul>"


def flow(items: list[tuple[str, str]], highlight: set[int] | None = None, caps: bool = False) -> str:
    highlight = highlight or set()
    parts = []
    for i, (t, s) in enumerate(items):
        if i:
            parts.append('<div class="arr">›</div>')
        hi = " hi" if i in highlight else ""
        cap = " caps" if caps else ""
        parts.append(f'<div class="fc{hi}"><div class="ft{cap}">{t}</div><div class="fs">{s}</div></div>')
    return '<div class="flow">' + "".join(parts) + "</div>"


def kpis(cards: list[tuple[str, str, str]]) -> str:
    out = []
    for label, value, note in cards:
        out.append(f'<div class="kpi"><div class="kl">{label}</div><div class="kv">{value}</div><div class="kn">{note}</div></div>')
    return '<div class="kpis">' + "".join(out) + "</div>"


def bars(rows: list[tuple[str, list[tuple[float, str]], str]], label_width: int = 150) -> str:
    """rows: (label, [(fraction, colour), ...] stacked left to right, right-hand value text)."""
    out = [f'<div class="bars" style="grid-template-columns:{label_width}px 1fr 48px">']
    for label, segs, val in rows:
        fill = "".join(
            f'<div class="bf{" stack" if len(segs) > 1 else ""}" style="width:{f * 100:.1f}%;background:{c}"></div>' for f, c in segs
        )
        out.append(f'<div class="bl">{label}</div><div class="bt">{fill}</div><div class="bv">{val}</div>')
    out.append("</div>")
    return "".join(out)


def panel(inner: str, title: str | None = None) -> str:
    t = f'<div class="pt">{title}</div>' if title else ""
    return f'<div class="panel">{t}{inner}</div>'


def code(text: str) -> str:
    return f'<pre class="code">{esc(text)}</pre>'


def page(number: int, kicker: str, title: str, subtitle: str, body: str) -> str:
    return f"""
<section class="page">
  <div class="topbar"></div>
  <header class="hdr"><span>SMARTFACTORY<span class="sep">/</span>SOLUTION BLUEPRINT</span>
    <span>PROJECT 02<span class="bar">|</span>{ISSUE_MONTH}<span class="bar">|</span><span class="ver">{VERSION_LABEL}</span></span></header>
  <main>
    <div class="kicker">Project blueprint / {kicker}</div>
    <h1>{title}</h1>
    <p class="sub">{subtitle}</p>
    <hr class="rule">
    {body}
  </main>
  <footer><span>{FOOTER_TEXT}</span><span>{number:02d}</span></footer>
</section>"""


def build(fonts_css: str) -> str:
    from content_a import PAGES as A  # noqa: PLC0415
    from content_b import PAGES as B  # noqa: PLC0415
    from content_c import PAGES as C  # noqa: PLC0415

    pages = A + B + C
    body = "".join(fn(i + 1) for i, fn in enumerate(pages))
    return f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<title>SmartFactory AI — Solution Blueprint {VERSION_LABEL}</title>
<style>{CSS(fonts_css)}</style></head>
<body>{body}</body></html>"""


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--fonts-css", default="")
    args = ap.parse_args()
    fonts_css = Path(args.fonts_css).read_text(encoding="utf-8") if args.fonts_css else ""
    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(build(fonts_css), encoding="utf-8")
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
