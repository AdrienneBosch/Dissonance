# Theming

Dissonance ships with light and dark themes so users can pick the most comfortable contrast profile.

![Home page – light mode](../Dissonance/Assets/Wiki/home_page_light_mode.png)

## Switching Themes

- Use the theme toggle in the application toolbar to switch between light and dark modes.
- The setting persists between sessions and is stored in the user configuration file.

## Customizing Colors

Developers can introduce new color resources in `Resources/Styles.xaml`. The MVVM bindings reference these resources, so changes automatically propagate through the UI.

## Accessibility Tips

- Choose high-contrast color combinations that meet WCAG AA or AAA guidelines.
- Test theme changes with screen readers to ensure focus indicators remain clear.

![Home page – dark mode](../Dissonance/Assets/Wiki/home_page_dark_mode.png)
