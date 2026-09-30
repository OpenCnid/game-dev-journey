# Frame-spike experiment evidence

Files prefixed `prealignment-` are exploratory captures from source `029542486cf3ff26d6079cedcdb41fa4e57eacc3`, before the simulation warm-up alignment fix. They are retained for audit, not used as the final controlled comparison. Startup catch-up losses shifted the gameplay phase: the control recorded 320,000 projectile spawns while other 30-second captures recorded 399,984. Their equal wall-clock durations did not ensure equivalent attack phases.

Subsequent captures wait for both simulation and wall-clock warm-up and report start/end simulation times. Invocation metadata records the mode flags and visible-window setting. Hidden-effect modes are diagnostic only; they preserve gameplay simulation but omit a category's rendering preparation and drawing.
