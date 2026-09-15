import React from "react";

interface SearchBarProps {
  value: string;
  onChange: (value: string) => void;
}

export function SearchBar({ value, onChange }: SearchBarProps) {
  return (
    <input
      type="search"
      className="search-input"
      placeholder="Search assets (typos are fine — try “sloar” or “wnd”)"
      value={value}
      onChange={(e) => onChange(e.target.value)}
      aria-label="Search assets"
    />
  );
}
