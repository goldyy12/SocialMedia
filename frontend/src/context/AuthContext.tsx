import { createContext } from "react";
import { type AuthContextType } from "../types/Auth.ts";

export const AuthContext = createContext<AuthContextType | null>(null);
