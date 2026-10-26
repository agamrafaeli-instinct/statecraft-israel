- The HUD holds at the Settings screen's 125% and 150% UI scales (#40): below the smallest window the 100% layout is
  tested at (HudPresenter's new hud-tight, set from the panel's logical size; small phone windows reach it too) the
  bar's rows, the icon column and the panels tighten; on the tight three-row window the icon column becomes a row of the
  phone's 60 px discs under the bar (at least 24 px on screen down to 375x667) and a screen takes the window like a
  mobile modal, with the card's backdrop leaving the row's discs clear. The decision card starts right of the tight
  column; the info panel is a scrolling panel capped clear of the bar and the column; the Cabinet's side column slims so
  the seats list keeps its room; and the small text keeps its 12 px on-screen floor at every size. The seven layout
  suites that failed at 125%/150% now run at all three scales.
