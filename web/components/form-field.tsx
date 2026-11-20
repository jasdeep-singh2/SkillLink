type Props = {
  label: string;
  name: string;
  type?: string;
  defaultValue?: string;
  errors?: string[];
  required?: boolean;
  autoComplete?: string;
};

export function FormField({ label, name, type = "text", defaultValue, errors, required, autoComplete }: Props) {
  const errorId = `${name}-error`;
  return (
    <label className="flex flex-col gap-1 text-sm">
      <span className="font-medium">{label}</span>
      <input
        name={name}
        type={type}
        defaultValue={defaultValue}
        required={required}
        autoComplete={autoComplete}
        aria-invalid={errors ? true : undefined}
        aria-describedby={errors ? errorId : undefined}
        className="rounded-md border border-black/15 bg-transparent px-3 py-2 dark:border-white/20"
      />
      {errors && (
        <span id={errorId} className="text-red-600 dark:text-red-400">
          {errors.join(" ")}
        </span>
      )}
    </label>
  );
}

export const buttonClass =
  "rounded-md bg-foreground px-4 py-2 text-sm font-medium text-background disabled:opacity-50";
