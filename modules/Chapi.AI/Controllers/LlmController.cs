using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chapi.AI.Services;
using Chapi.EndpointCatalog.Application;
using Chapi.AI.Dto;

namespace Chapi.AI.Controllers
{
    [ApiController]
    [Route("api/llm")]
    public class LlmController : ControllerBase
    {
        private readonly IApiTestGenerationService _apiTestService;
        private readonly ILogger<LlmController> _logger;
        private readonly IEndpointContextService _endpointContextService;
        private readonly Chapi.AI.Services.IAuthAiDetectionService _authDetectionService;

        public LlmController(IApiTestGenerationService apiTestService, IEndpointContextService endpointContextService, ILogger<LlmController> logger, Chapi.AI.Services.IAuthAiDetectionService authDetectionService)
        {
            _apiTestService = apiTestService;
            _endpointContextService = endpointContextService;
            _logger = logger;
            _authDetectionService = authDetectionService;
        }


        [HttpPost("generate")]
        public async Task<ChapiCard> Generate([FromBody] Chapi.AI.Dto.ApiTestGenerateRequest req)
        {
            _logger.LogInformation("LLM generate requested (Chapi.AI controller)");

            var endpointsContext = await _endpointContextService.BuildContextAsync(req.ProjectId);
            var card = await _apiTestService.GenerateTestAsync(req.UserQuery, endpointsContext, req.MaxFiles, req.OpenApiJson);

            return card;
        }

        [HttpPost("detect/code")]
        public async Task<DetectionResponseDto> DetectByCode([FromBody] DetectByCodeRequest req, System.Threading.CancellationToken ct)
        {
            _logger.LogInformation("LLM detect by code requested");
            var code = req.Code ?? string.Empty;
            code = "' import axios from \\\\\\'axios\\\\\\'; import { config, oauthConfig } from \\\\\\'../config/app.config\\\\\\'; import type { LoginDto, UserDto, AuthResultDto } from \\\\\\'@/types/auth-pilot\\\\\\'; interface TokenResponse { access_token: string; token_type: string; expires_in: number; refresh_token?: string; scope?: string; id_token?: string; } export class AuthService { private static get tokenKey() { return oauthConfig.storageKeys.accessToken; } private static get refreshTokenKey() { return oauthConfig.storageKeys.refreshToken; } private static get userKey() { return oauthConfig.storageKeys.user; } private static get idTokenKey() { return oauthConfig.storageKeys.idToken; } private static get expiresKey() { return oauthConfig.storageKeys.expiresAt; } static async login(credentials: LoginDto): Promise<AuthResultDto> { try { const formData = new URLSearchParams(); formData.append(\\\\\\'grant_type\\\\\\', oauthConfig.grantTypes.password); formData.append(\\\\\\'client_id\\\\\\', config.authConfig.clientId); formData.append(\\\\\\'username\\\\\\', credentials.email); formData.append(\\\\\\'password\\\\\\', credentials.password); formData.append(\\\\\\'scope\\\\\\', config.authConfig.scopes); const response = await axios.post<TokenResponse>( config.authConfig.tokenEndpoint, formData, { baseURL: config.apiUrl, headers: { \\\\\\'Content-Type\\\\\\': \\\\\\'application/x-www-form-urlencoded\\\\\\', }, } ); const tokenData = response.data; if (tokenData.access_token) { localStorage.setItem(this.tokenKey, tokenData.access_token); if (tokenData.refresh_token) { localStorage.setItem(this.refreshTokenKey, tokenData.refresh_token); } if (tokenData.id_token) { localStorage.setItem(this.idTokenKey, tokenData.id_token); } const expiresAt = Date.now() + (tokenData.expires_in * 1000); localStorage.setItem(this.expiresKey, expiresAt.toString()); const user = await this.getUserInfo(); if (user) { localStorage.setItem(this.userKey, JSON.stringify(user)); } return { success: true, token: tokenData.access_token, user: user }; } return { success: false, errorMessage: \\\\\\'No access token received\\\\\\' }; } catch (error) { console.error(\\\\\\'Login error:\\\\\\', error); if (axios.isAxiosError(error)) { if (error.response?.status === 400) { return { success: false, errorMessage: \\\\\\'Invalid email or password\\\\\\' }; } if (error.response?.status === 401) { return { success: false, errorMessage: \\\\\\'Invalid email or password\\\\\\' }; } if (error.code === \\\\\\'ECONNREFUSED\\\\\\' || error.message.includes(\\\\\\'ERR_CONNECTION_REFUSED\\\\\\')) { return { success: false, errorMessage: \\\\\\'Cannot connect to server. Please check if the backend is running.\\\\\\' }; } return { success: false, errorMessage: `Server error: ${error.response?.status || \\\\\\'Unknown\\\\\\'}` }; } return { success: false, errorMessage: \\\\\\'Network error during authentication\\\\\\' }; } } private static async getUserInfo(): Promise<UserDto | null> { try { const idToken = localStorage.getItem(this.idTokenKey); console.log(\\\\\\'ID Token available:\\\\\\', !!idToken); if (!idToken) { console.warn(\\\\\\'No id_token found\\\\\\'); return null; } const payload = JSON.parse(atob(idToken.split(\\\\\\'.\\\\\\')[1])); console.log(\\\\\\'ID Token payload:\\\\\\', payload); const user = { id: payload.sub, email: payload.email || payload.username, username: payload.preferred_username || payload.email || payload.username, name: payload.given_name || payload.name || \\\\\\'\\\\\\', surname: payload.family_name || \\\\\\'\\\\\\', roles: payload.role ? (Array.isArray(payload.role) ? payload.role : [payload.role]) : [], isActive: true, isEmailConfirmed: true, isPhoneNumberConfirmed: false, isLockoutEnabled: false, createdAt: new Date().toISOString(), }; console.log(\\\\\\'Extracted user info:\\\\\\', user); return user; } catch (error) { console.error(\\\\\\'Error decoding id_token:\\\\\\', error); return null; } } static async logout(): Promise<void> { try { this.clearAuth(); } catch (error) { console.error(\\\\\\'Logout error:\\\\\\', error); } } static async refreshToken(): Promise<AuthResultDto | null> { const refreshToken = localStorage.getItem(this.refreshTokenKey); if (!refreshToken) return null; try { const formData = new URLSearchParams(); formData.append(\\\\\\'grant_type\\\\\\', oauthConfig.grantTypes.refreshToken); formData.append(\\\\\\'client_id\\\\\\', config.authConfig.clientId); formData.append(\\\\\\'refresh_token\\\\\\', refreshToken); const response = await axios.post<TokenResponse>( config.authConfig.tokenEndpoint, formData, { baseURL: config.apiUrl, headers: { \\\\\\'Content-Type\\\\\\': \\\\\\'application/x-www-form-urlencoded\\\\\\', }, } ); const tokenData = response.data; if (tokenData.access_token) { localStorage.setItem(this.tokenKey, tokenData.access_token); if (tokenData.refresh_token) { localStorage.setItem(this.refreshTokenKey, tokenData.refresh_token); } const expiresAt = Date.now() + (tokenData.expires_in * 1000); localStorage.setItem(this.expiresKey, expiresAt.toString()); const user = await this.getUserInfo(); if (user) { localStorage.setItem(this.userKey, JSON.stringify(user)); } return { success: true, token: tokenData.access_token, user: user }; } this.clearAuth(); return null; } catch (error) { console.error(\\\\\\'Token refresh error:\\\\\\', error); this.clearAuth(); return null; } } static getToken(): string | null { const token = localStorage.getItem(this.tokenKey); const expiresAt = localStorage.getItem(this.expiresKey); if (token && expiresAt) { const expiry = parseInt(expiresAt, 10); if (Date.now() >= expiry) { this.clearAuth(); return null; } } return token; } static getUser(): UserDto | null { const userStr = localStorage.getItem(this.userKey); if (!userStr) return null; try { return JSON.parse(userStr) as UserDto; } catch { return null; } } static isAuthenticated(): boolean { return !!this.getToken() && !!this.getUser(); } static clearAuth(): void { localStorage.removeItem(this.tokenKey); localStorage.removeItem(this.refreshTokenKey); localStorage.removeItem(this.idTokenKey); localStorage.removeItem(this.userKey); localStorage.removeItem(this.expiresKey); } static async authenticatedFetch<T>( url: string, options: Record<string, unknown> = {} ): Promise<T> { let token = this.getToken(); if (!token) { const refreshResult = await this.refreshToken(); if (refreshResult && refreshResult.success) { token = this.getToken(); } if (!token) { this.clearAuth(); window.location.href = \\\\\\'/auth/login\\\\\\'; throw new Error(\\\\\\'No authentication token available\\\\\\'); } } const headers = { \\\\\\'Authorization\\\\\\': `Bearer ${token}`, \\\\\\'Content-Type\\\\\\': \\\\\\'application/json\\\\\\', ...(options.headers as Record<string, string> || {}), }; try { const response = await axios({ url, baseURL: config.apiUrl, headers, ...options, }); return response.data; } catch (error) { if (axios.isAxiosError(error) && error.response?.status === 401) { const refreshResult = await this.refreshToken(); if (refreshResult && refreshResult.success) { const newHeaders = { ...headers, \\\\\\'Authorization\\\\\\': `Bearer ${this.getToken()}`, }; const retryResponse = await axios({ url, baseURL: config.apiUrl, headers: newHeaders, ...options, }); return retryResponse.data; } else { this.clearAuth(); window.location.href = \\\\\\'/auth/login\\\\\\'; throw new Error(\\\\\\'Authentication failed\\\\\\'); } } throw error; } } } export const config = { apiUrl: import.meta.env.VITE_API_BASE_URL || import.meta.env.VITE_API_URL || \\\\\\'http://localhost:5002\\\\\\', authConfig: { tokenEndpoint: \\\\\\'/connect/token\\\\\\', scopes: \\\\\\'openid email profile roles\\\\\\', clientId: import.meta.env.VITE_AUTH_CLIENT_ID || \\\\\\'spa-client\\\\\\', } }; export const oauthConfig = { grantTypes: { password: \\\\\\'password\\\\\\', refreshToken: \\\\\\'refresh_token\\\\\\', }, storageKeys: { accessToken: \\\\\\'access_token\\\\\\', refreshToken: \\\\\\'refresh_token\\\\\\', idToken: \\\\\\'id_token\\\\\\', user: \\\\\\'user\\\\\\', expiresAt: \\\\\\'token_expires_at\\\\\\', } }; '\r\n";
            var res = await _authDetectionService.DetectByCodeAsync(code, req.ProjectId, ct);
            return res;
        }

        [HttpPost("detect/prompt")]
        public async Task<DetectionResponseDto> DetectByPrompt([FromBody] DetectByPromptRequest req, System.Threading.CancellationToken ct)
        {
            _logger.LogInformation("LLM detect by prompt requested");
            var res = await _authDetectionService.DetectByPromptAsync(req.Prompt ?? string.Empty, req.ProjectId, ct);
            return res;
        }

    }
}
