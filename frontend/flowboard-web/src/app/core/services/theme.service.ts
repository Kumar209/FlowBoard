import { Injectable, signal, effect } from '@angular/core';

export type Theme =
  | 'light'
  | 'dark'
  | 'cupcake'
  | 'bumblebee'
  | 'emerald'
  | 'corporate'
  | 'synthwave'
  | 'retro'
  | 'cyberpunk'
  | 'valentine'
  | 'halloween'
  | 'garden'
  | 'forest'
  | 'aqua'
  | 'lofi'
  | 'pastel'
  | 'fantasy'
  | 'wireframe'
  | 'black'
  | 'luxury'
  | 'dracula'
  | 'cmyk'
  | 'autumn'
  | 'business'
  | 'acid'
  | 'lemonade'
  | 'night'
  | 'coffee'
  | 'winter'
  | 'dim'
  | 'nord'
  | 'sunset';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {

  themes: { value: Theme; label: string }[] = [
    { value: 'light', label: 'Light' },
    { value: 'dark', label: 'Dark' },
    { value: 'cupcake', label: 'Cupcake' },
    { value: 'bumblebee', label: 'Bumblebee' },
    { value: 'emerald', label: 'Emerald' },
    { value: 'corporate', label: 'Corporate' },
    { value: 'synthwave', label: 'Synthwave' },
    { value: 'retro', label: 'Retro' },
    { value: 'cyberpunk', label: 'Cyberpunk' },
    { value: 'valentine', label: 'Valentine' },
    { value: 'halloween', label: 'Halloween' },
    { value: 'garden', label: 'Garden' },
    { value: 'forest', label: 'Forest' },
    { value: 'aqua', label: 'Aqua' },
    { value: 'lofi', label: 'Lo-Fi' },
    { value: 'pastel', label: 'Pastel' },
    { value: 'fantasy', label: 'Fantasy' },
    { value: 'wireframe', label: 'Wireframe' },
    { value: 'black', label: 'Black' },
    { value: 'luxury', label: 'Luxury' },
    { value: 'dracula', label: 'Dracula' },
    { value: 'cmyk', label: 'CMYK' },
    { value: 'autumn', label: 'Autumn' },
    { value: 'business', label: 'Business' },
    { value: 'acid', label: 'Acid' },
    { value: 'lemonade', label: 'Lemonade' },
    { value: 'night', label: 'Night' },
    { value: 'coffee', label: 'Coffee' },
    { value: 'winter', label: 'Winter' },
    { value: 'dim', label: 'Dim' },
    { value: 'nord', label: 'Nord' },
    { value: 'sunset', label: 'Sunset' },
  ];

  currentTheme = signal<Theme>(
    (localStorage.getItem('flowboard-theme') as Theme) || 'corporate'
  );

  constructor() {
    effect(() => {
      const theme = this.currentTheme();

      document.documentElement.setAttribute('data-theme', theme);
      localStorage.setItem('flowboard-theme', theme);
    });

    // Initialize theme immediately on load
    document.documentElement.setAttribute(
      'data-theme',
      this.currentTheme()
    );
  }

  setTheme(theme: Theme) {
    this.currentTheme.set(theme);
  }
}