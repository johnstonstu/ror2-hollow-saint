# Gaze presentation adapter checks

Imported from Astra's contrast candidate validation harness. The project links
the combined candidate's production presentation files. Physics, rendering,
body and palette adapters check bounded geometry, event timing, cleanup,
contrast rules and reuse over 1,000 casts. They do not validate Unity rendering,
actual terrain, networking, material bloom or gameplay performance.

Run `dotnet run --project tools/GazePresentationChecks -c Release`.
